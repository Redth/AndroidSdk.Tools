using System.Linq;
using Xunit;

namespace AndroidSdk.Tests;

/// <summary>
/// Unit tests for SdkManager.ParseListOutput to ensure progress bars,
/// Info: lines, and other diagnostic output are not parsed as packages.
/// </summary>
public class SdkManager_ListParsing_Tests
{
	private static SdkManager CreateParser()
	{
		// SdkManager with SkipVersionCheck so it doesn't need a real SDK
		return new SdkManager(new SdkManagerToolOptions { SkipVersionCheck = true });
	}

	[Fact]
	public void ParseListOutput_ProgressBarsAreNotParsedAsPackages()
	{
		// Simulates the exact output from `sdkmanager --list --verbose` where
		// progress bars using \r get concatenated into a single line by ProcessRunner.
		var lines = new[]
		{
			"Loading package information...",
			"Loading local repository...",
			"Info: Parsing /sdk/build-tools/36.0.0/package.xml",
			"Info: Parsing /sdk/platforms/android-36/package.xml",
			"[=========                              ] 25% Loading local repository...       \r[=========                              ] 25% Fetch remote repository...        \r[=======================================] 100% Computing updates...             \r",
			"Installed packages:",
			"--------------------------------------",
			"build-tools;36.0.0",
			"    Description:        Android SDK Build-Tools 36",
			"    Version:            36.0.0",
			"    Installed Location: /sdk/build-tools/36.0.0",
			"",
			"platforms;android-36",
			"    Description:        Android SDK Platform 36",
			"    Version:            1",
			"    Installed Location: /sdk/platforms/android-36",
		};

		var parser = CreateParser();
		var result = parser.ParseListOutput(lines);

		Assert.Equal(2, result.InstalledPackages.Count);
		Assert.Equal("build-tools;36.0.0", result.InstalledPackages[0].Path);
		Assert.Equal("platforms;android-36", result.InstalledPackages[1].Path);
	}

	[Fact]
	public void ParseListOutput_ProgressBarsInterleavedAfterHeader_AreFiltered()
	{
		// Worst case: due to async stdout/stderr merging, progress/diagnostic
		// lines end up after the "Installed packages:" header.
		var lines = new[]
		{
			"Installed packages:",
			"--------------------------------------",
			"[=========                              ] 25% Fetch remote repository...        \r[=======================================] 100% Computing updates...             \r",
			"Info: Parsing /sdk/build-tools/36.0.0/package.xml",
			"Loading local repository...",
			"Warning: package.xml parsing problem.",
			"build-tools;36.0.0",
			"    Description:        Android SDK Build-Tools 36",
			"    Version:            36.0.0",
			"    Installed Location: /sdk/build-tools/36.0.0",
		};

		var parser = CreateParser();
		var result = parser.ParseListOutput(lines);

		Assert.Single(result.InstalledPackages);
		Assert.Equal("build-tools;36.0.0", result.InstalledPackages[0].Path);
		Assert.Equal("Android SDK Build-Tools 36", result.InstalledPackages[0].Description);
	}

	[Fact]
	public void ParseListOutput_AvailablePackages_AreNotAffected()
	{
		var lines = new[]
		{
			"Loading package information...",
			"[=======================================] 100% Computing updates...             \r",
			"Installed packages:",
			"--------------------------------------",
			"emulator",
			"    Description:        Android Emulator",
			"    Version:            36.4.9",
			"    Installed Location: /sdk/emulator",
			"",
			"Available Packages:",
			"--------------------------------------",
			"build-tools;35.0.0",
			"    Description:        Android SDK Build-Tools 35",
			"    Version:            35.0.0",
			"",
			"ndk;27.0.0",
			"    Description:        NDK",
			"    Version:            27.0.0",
		};

		var parser = CreateParser();
		var result = parser.ParseListOutput(lines);

		Assert.Single(result.InstalledPackages);
		Assert.Equal("emulator", result.InstalledPackages[0].Path);

		// The first available package is committed when the second path is encountered
		Assert.True(result.AvailablePackages.Count >= 1);
		Assert.Equal("build-tools;35.0.0", result.AvailablePackages[0].Path);
	}

	[Fact]
	public void ParseListOutput_EmptyOutput_ReturnsEmptyLists()
	{
		var parser = CreateParser();
		var result = parser.ParseListOutput(System.Array.Empty<string>());

		Assert.Empty(result.InstalledPackages);
		Assert.Empty(result.AvailablePackages);
	}

	[Fact]
	public void ParseListOutput_OnlyProgressBars_ReturnsEmptyLists()
	{
		var lines = new[]
		{
			"Loading package information...",
			"Loading local repository...",
			"Info: Parsing /sdk/emulator/package.xml",
			"[=========                              ] 25% Loading local repository...       \r[=======================================] 100% Computing updates...             \r",
		};

		var parser = CreateParser();
		var result = parser.ParseListOutput(lines);

		Assert.Empty(result.InstalledPackages);
		Assert.Empty(result.AvailablePackages);
	}
}
