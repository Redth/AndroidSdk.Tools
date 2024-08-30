#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AndroidSdk;

public class SdkLocator : PathLocator
{
	public override string[] PreferredPaths()
	{
		var paths = new List<string>();

		if (OperatingSystem.IsWindows()) {
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

		var androidSdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
		if (!string.IsNullOrEmpty(androidSdkRoot))
			paths.Add(androidSdkRoot);

		var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
		if (!string.IsNullOrEmpty(androidHome))
			paths.Add(androidHome);

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
		] :
		[
			// Xamarin.Android seems to check this path first
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Android", "sdk"),

			// These are other known paths
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "android-sdk-macosx"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "Xamarin", "android-sdk-macosx"),
			Path.Combine("Developer", "Android", "android-sdk-macosx"),
		];

	
}
