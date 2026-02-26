#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AndroidSdk;

public class SdkLocator : PathLocator
{
	protected bool IsWindows
			=> RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

	public override string[] PreferredPaths()
	{
		var paths = new List<string>();

		if (IsWindows) {
			// Try the registry entries known by the Xamarin SDK
			var registryConfig = MonoDroidSdkLocator.ReadRegistry();
			if (!string.IsNullOrEmpty(registryConfig.AndroidSdkPath))
				paths.Add(registryConfig.AndroidSdkPath);
		}
		else
		{
			// Try the monodroid-config.xml file known by the Xamarin SDK
			var monodroidConfig = MonoDroidSdkLocator.ReadConfigFile();
			if (!string.IsNullOrEmpty(monodroidConfig.AndroidSdkPath))
				paths.Add(monodroidConfig.AndroidSdkPath);
		}

		var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
		if (!string.IsNullOrEmpty(androidHome))
			paths.Add(androidHome);

		// ANDROID_SDK_ROOT is deprecated in favor of ANDROID_HOME
		var androidSdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
		if (!string.IsNullOrEmpty(androidSdkRoot))
			paths.Add(androidSdkRoot);

		return paths.ToArray();
	}

	public override string[] AdditionalPaths()
		=> RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
		[
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "android-sdk"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".android"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".android", "sdk"),
		] :
		[
			// Xamarin.Android seems to check this path first
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Android", "sdk"),

			// These are other known paths
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "android-sdk-macosx"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "Xamarin", "android-sdk-macosx"),
			Path.Combine("Developer", "Android", "android-sdk-macosx"),
			
			// Linux possibilities
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "sdk"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Android"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Android", "sdk"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Android", "Sdk"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "android"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "android", "sdk"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "android", "Sdk"),
		];

	/// <summary>
	/// Checks whether the given SDK directory contains the cmdline-tools component.
	/// </summary>
	internal static bool HasCmdlineTools(string sdkPath)
	{
		var cmdlineToolsDir = Path.Combine(sdkPath, "cmdline-tools");
		if (!Directory.Exists(cmdlineToolsDir))
			return false;

		// Check that at least one version subdirectory exists with the sdkmanager binary
		try
		{
			foreach (var dir in Directory.GetDirectories(cmdlineToolsDir))
			{
				var sdkmanager = Path.Combine(dir, "bin", "sdkmanager");
				var sdkmanagerBat = Path.Combine(dir, "bin", "sdkmanager.bat");
				if (File.Exists(sdkmanager) || File.Exists(sdkmanagerBat))
					return true;
			}
		}
		catch { }

		return false;
	}

	/// <summary>
	/// Overrides base Locate to rank SDKs by completeness.
	/// SDKs with cmdline-tools are preferred over those without.
	/// A user-specified path (specificHome) always takes priority.
	/// </summary>
	public new IReadOnlyList<DirectoryInfo> Locate(string? specificHome = null, params string[]? additionalPossibleDirectories)
	{
		var basePaths = base.Locate(specificHome, additionalPossibleDirectories);

		if (basePaths.Count <= 1)
			return basePaths;

		// If the user explicitly specified a home, keep it first and only rank the rest
		int skipCount = 0;
		var fixedPaths = new List<DirectoryInfo>();

		if (!string.IsNullOrEmpty(specificHome))
		{
			var specificDir = basePaths.FirstOrDefault(p =>
				p.FullName.Equals(specificHome, StringComparison.OrdinalIgnoreCase) ||
				p.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar).Equals(specificHome.TrimEnd(System.IO.Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase));

			if (specificDir is not null)
			{
				fixedPaths.Add(specificDir);
				skipCount = 1;
			}
		}

		// Stable sort remaining: SDKs with cmdline-tools come first
		var remaining = basePaths.Where(p => !fixedPaths.Contains(p)).ToList();
		var withTools = new List<DirectoryInfo>();
		var withoutTools = new List<DirectoryInfo>();

		foreach (var path in remaining)
		{
			if (HasCmdlineTools(path.FullName))
				withTools.Add(path);
			else
				withoutTools.Add(path);
		}

		var result = new List<DirectoryInfo>(basePaths.Count);
		result.AddRange(fixedPaths);
		result.AddRange(withTools);
		result.AddRange(withoutTools);
		return result;
	}
}
