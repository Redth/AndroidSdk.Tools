#nullable enable
using System;
using System.IO;
using Xunit;

namespace AndroidSdk.Tests;

/// <summary>
/// Tests AVD location precedence and AVD file discovery behavior.
/// </summary>
public class AvdLocator_Tests
{
	[Fact]
	public void LocatedPathForAndroidSdkHomeIncludesDotAndroid()
	{
		var tempRoot = CreateTempRoot(nameof(LocatedPathForAndroidSdkHomeIncludesDotAndroid));
		var expectedPath = Path.Combine(tempRoot, ".android", "avd");
		Directory.CreateDirectory(expectedPath);
		using var envVars = new EnvironmentVariablesScope(
			("ANDROID_AVD_ROOT", null),
			("ANDROID_AVD_HOME", null),
			("ANDROID_USER_HOME", null),
			("ANDROID_PREFS_ROOT", null),
			("ANDROID_SDK_HOME", tempRoot));

		try
		{
			var l = new AvdLocator();
			var paths = l.PreferredPaths();

			Assert.Equal(expectedPath, paths[0]);
		}
		finally
		{
			CleanupDirectory(tempRoot);
		}
	}

	[Fact]
	public void LocatedPathForAndroidPrefsRootIncludesDotAndroid()
	{
		var tempRoot = CreateTempRoot(nameof(LocatedPathForAndroidPrefsRootIncludesDotAndroid));
		var expectedPath = Path.Combine(tempRoot, ".android", "avd");
		Directory.CreateDirectory(expectedPath);
		using var envVars = new EnvironmentVariablesScope(
			("ANDROID_AVD_ROOT", null),
			("ANDROID_AVD_HOME", null),
			("ANDROID_USER_HOME", null),
			("ANDROID_PREFS_ROOT", tempRoot));

		try
		{
			var l = new AvdLocator();
			var paths = l.PreferredPaths();

			Assert.Equal(expectedPath, paths[0]);
		}
		finally
		{
			CleanupDirectory(tempRoot);
		}
	}

	[Fact]
	public void LocatedPathForAndroidUserHomeUsesAvdSuffix()
	{
		var tempRoot = CreateTempRoot(nameof(LocatedPathForAndroidUserHomeUsesAvdSuffix));
		var expectedPath = Path.Combine(tempRoot, "avd");
		Directory.CreateDirectory(expectedPath);
		using var envVars = new EnvironmentVariablesScope(
			("ANDROID_AVD_ROOT", null),
			("ANDROID_AVD_HOME", null),
			("ANDROID_USER_HOME", tempRoot));

		try
		{
			var l = new AvdLocator();
			var paths = l.PreferredPaths();

			Assert.Equal(expectedPath, paths[0]);
		}
		finally
		{
			CleanupDirectory(tempRoot);
		}
	}

	[Fact]
	public void ListAvdsFindsAvdsFromFileSystem()
	{
		var tempRoot = CreateTempRoot(nameof(ListAvdsFindsAvdsFromFileSystem));
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
			CleanupDirectory(tempRoot);
		}
	}

	static string CreateTempRoot(string testName)
		=> Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AvdLocator_Tests), testName);

	static void CleanupDirectory(string path)
	{
		try
		{
			if (Directory.Exists(path))
				Directory.Delete(path, true);
		}
		catch (IOException) { }
		catch (UnauthorizedAccessException) { }
	}
}
