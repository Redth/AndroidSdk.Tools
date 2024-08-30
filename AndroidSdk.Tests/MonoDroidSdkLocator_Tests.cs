using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class MonoDroidSdkLocator_Tests : TestsBase
{
	public MonoDroidSdkLocator_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void LocatePaths()
	{
		// File may not exist, which is an acceptable outcome too
		if (File.Exists(MonoDroidSdkLocator.MonoDroidConfigXmlFilename))
		{
			var location = MonoDroidSdkLocator.LocatePaths();

			Assert.NotNull(location.JavaJdkPath);
			Assert.NotNull(location.AndroidSdkPath);
		}
	}

	[Fact]
	public void WritePathsWithConfigFile()
	{
		// Runs with config file on any platform so we can test on windows more easily too
		// even though the config file is not ever actually used for windows
		WriteConfigImpl(true);
	}

	[Fact]
	public void WritePathsWindows()
	{
		// No need to run the non-windows path here since it would use config file
		// which we already have a test for
		if (!OperatingSystem.IsWindows())
			return;

		WriteConfigImpl(false);
	}

	void WriteConfigImpl(bool forceConfigFile)
	{
		// First read the current values
		var originalPaths = MonoDroidSdkLocator.LocatePaths(forceConfigFile);

		// Change value to test it works
		var invalidPaths = new MonoDroidSdkLocation("WRONGSDK", "WRONGJDK");
		MonoDroidSdkLocator.UpdatePaths(invalidPaths, forceConfigFile);

		// Get the new values back to confirm
		var updatedPaths = MonoDroidSdkLocator.LocatePaths(forceConfigFile);

		// Reset the values back to the original
		MonoDroidSdkLocator.UpdatePaths(originalPaths, forceConfigFile);

		// Assert our update worked
		Assert.Equal(invalidPaths.JavaJdkPath, updatedPaths.JavaJdkPath);
		Assert.Equal(invalidPaths.AndroidSdkPath, updatedPaths.AndroidSdkPath);
	}
}
