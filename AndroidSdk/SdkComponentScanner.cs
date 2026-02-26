#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AndroidRepository.Common_2;

namespace AndroidSdk;

/// <summary>
/// Represents an installed component found in an Android SDK directory.
/// </summary>
public class InstalledComponent
{
	/// <summary>
	/// The SDK package path (e.g., "platform-tools", "build-tools;36.1.0", "cmdline-tools;19.0").
	/// </summary>
	public string Path { get; set; } = string.Empty;

	/// <summary>
	/// Human-readable display name (e.g., "Android SDK Platform-Tools").
	/// </summary>
	public string? DisplayName { get; set; }

	/// <summary>
	/// The package version.
	/// </summary>
	public Version? Version { get; set; }

	/// <summary>
	/// The directory where this component is installed.
	/// </summary>
	public DirectoryInfo? Location { get; set; }
}

/// <summary>
/// Represents the inventory of components installed in an Android SDK directory,
/// discovered by scanning package.xml files (no sdkmanager required).
/// </summary>
public class SdkInventory
{
	/// <summary>
	/// The root directory of the SDK.
	/// </summary>
	public DirectoryInfo? SdkHome { get; set; }

	/// <summary>
	/// All installed components found by scanning package.xml files.
	/// </summary>
	public List<InstalledComponent> Components { get; set; } = new();

	/// <summary>Whether cmdline-tools is installed (provides sdkmanager and avdmanager).</summary>
	public bool HasCmdlineTools => Components.Any(c => c.Path.StartsWith("cmdline-tools"));

	/// <summary>Whether platform-tools is installed (provides adb).</summary>
	public bool HasPlatformTools => Components.Any(c => c.Path == "platform-tools");

	/// <summary>Whether the emulator is installed.</summary>
	public bool HasEmulator => Components.Any(c => c.Path == "emulator");

	/// <summary>Whether any build-tools version is installed.</summary>
	public bool HasBuildTools => Components.Any(c => c.Path.StartsWith("build-tools;"));

	/// <summary>Whether any platform (API level) is installed.</summary>
	public bool HasPlatforms => Components.Any(c => c.Path.StartsWith("platforms;"));

	/// <summary>Whether any system image is installed.</summary>
	public bool HasSystemImages => Components.Any(c => c.Path.StartsWith("system-images;"));
}

/// <summary>
/// Scans an Android SDK directory for installed components by parsing package.xml files.
/// This works even when cmdline-tools (sdkmanager) is not installed.
/// </summary>
public class SdkComponentScanner
{
	static readonly XmlSerializer serializer = new XmlSerializer(typeof(Repository));

	/// <summary>
	/// Scans the specified SDK directory for installed components.
	/// </summary>
	/// <param name="sdkHome">The root directory of the Android SDK.</param>
	/// <returns>An inventory of installed components.</returns>
	public SdkInventory Scan(DirectoryInfo? sdkHome)
	{
		var inventory = new SdkInventory { SdkHome = sdkHome };

		if (sdkHome is null || !sdkHome.Exists)
			return inventory;

		var packageXmlFiles = sdkHome.GetFiles("package.xml", SearchOption.AllDirectories);

		foreach (var packageXml in packageXmlFiles)
		{
			try
			{
				var component = ParsePackageXml(packageXml);
				if (component is not null)
					inventory.Components.Add(component);
			}
			catch
			{
				// Skip files that can't be parsed
			}
		}

		return inventory;
	}

	static InstalledComponent? ParsePackageXml(FileInfo packageXml)
	{
		using var stream = packageXml.OpenRead();
		var repo = serializer.Deserialize(stream) as Repository;
		var localPackage = repo?.LocalPackage;

		if (localPackage is null || string.IsNullOrEmpty(localPackage.Path))
			return null;

		Version? version = null;
		if (localPackage.Revision is not null)
		{
			try
			{
				var rev = localPackage.Revision;
				version = rev.MicroSpecified
					? new Version(rev.Major, rev.MinorSpecified ? rev.Minor : 0, rev.Micro)
					: rev.MinorSpecified
						? new Version(rev.Major, rev.Minor)
						: new Version(rev.Major, 0);
			}
			catch { }
		}

		return new InstalledComponent
		{
			Path = localPackage.Path,
			DisplayName = localPackage.DisplayName,
			Version = version,
			Location = packageXml.Directory,
		};
	}
}
