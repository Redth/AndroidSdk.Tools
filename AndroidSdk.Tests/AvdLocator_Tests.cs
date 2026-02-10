using System;
using System.IO;
using Xunit;

namespace AndroidSdk.Tests;

public class AvdLocator_Tests
{
	[Fact]
	public void LocatedPathForAndroidSdkHomeIncludesDotAndroid()
	{
		var oldVal = Environment.GetEnvironmentVariable("ANDROID_SDK_HOME");

		var tempRoot = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AvdLocator_Tests), nameof(LocatedPathForAndroidSdkHomeIncludesDotAndroid));
		var expectedPath = Path.Combine(tempRoot, ".android", "avd");
		Directory.CreateDirectory(expectedPath);

		try
		{
			Environment.SetEnvironmentVariable("ANDROID_SDK_HOME", tempRoot);

			var l = new AvdLocator();
			var paths = l.PreferredPaths();

			Assert.Contains(expectedPath, paths);
		}
		finally
		{
			Environment.SetEnvironmentVariable("ANDROID_SDK_HOME", oldVal);
			if (Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, true);
		}
	}

	[Fact]
	public void LocatedPathForAndroidPrefsRootIncludesDotAndroid()
	{
		var oldVal = Environment.GetEnvironmentVariable("ANDROID_PREFS_ROOT");

		var tempRoot = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AvdLocator_Tests), nameof(LocatedPathForAndroidPrefsRootIncludesDotAndroid));
		var expectedPath = Path.Combine(tempRoot, ".android", "avd");
		Directory.CreateDirectory(expectedPath);

		try
		{
			Environment.SetEnvironmentVariable("ANDROID_PREFS_ROOT", tempRoot);

			var l = new AvdLocator();
			var paths = l.PreferredPaths();

			Assert.Contains(expectedPath, paths);
		}
		finally
		{
			Environment.SetEnvironmentVariable("ANDROID_PREFS_ROOT", oldVal);
			if (Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, true);
		}
	}

	[Fact]
	public void LocatedPathForAndroidUserHomeUsesAvdSuffix()
	{
		var oldVal = Environment.GetEnvironmentVariable("ANDROID_USER_HOME");

		var tempRoot = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AvdLocator_Tests), nameof(LocatedPathForAndroidUserHomeUsesAvdSuffix));
		var expectedPath = Path.Combine(tempRoot, "avd");
		Directory.CreateDirectory(expectedPath);

		try
		{
			Environment.SetEnvironmentVariable("ANDROID_USER_HOME", tempRoot);

			var l = new AvdLocator();
			var paths = l.PreferredPaths();

			Assert.Contains(expectedPath, paths);
		}
		finally
		{
			Environment.SetEnvironmentVariable("ANDROID_USER_HOME", oldVal);
			if (Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, true);
		}
	}

	[Fact]
	public void ListAvdsFindsAvdsFromFileSystem()
	{
		var tempRoot = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AvdLocator_Tests), nameof(ListAvdsFindsAvdsFromFileSystem));
		var avdHome = Path.Combine(tempRoot, "avd-home");

		// Create a fake AVD structure
		var avdDir = Path.Combine(avdHome, "TestDevice.avd");
		Directory.CreateDirectory(avdDir);
		File.WriteAllText(Path.Combine(avdHome, "TestDevice.ini"), "path=" + avdDir + "\ntarget=android-31\n");
		File.WriteAllText(Path.Combine(avdDir, "config.ini"), "hw.device.name=pixel\navd.ini.displayname=Test Device\n");

		try
		{
			var l = new AvdLocator();
			var avds = l.ListAvds(avdHome);

			Assert.Single(avds);
			Assert.Equal("TestDevice", avds[0].Name);
			Assert.Equal("pixel", avds[0].DeviceName);
			Assert.Equal("android-31", avds[0].Target);
		}
		finally
		{
			if (Directory.Exists(tempRoot))
				Directory.Delete(tempRoot, true);
		}
	}
}
