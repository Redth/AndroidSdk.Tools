using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

[Collection(AndroidSdkManagerCollection.Name)]
public class AvdManager_Tests : AndroidSdkManagerTestsBase, IClassFixture<AvdEnvironmentFixture>
{
	readonly AvdEnvironmentFixture avdEnvironment;

	static readonly string TestEmulatorName = "TestAvd" + Guid.NewGuid().ToString("N").Substring(0, 6);

	public AvdManager_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture, AvdEnvironmentFixture avdEnvironment)
		: base(outputHelper, fixture)
	{
		this.avdEnvironment = avdEnvironment;
		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", avdEnvironment.AndroidAvdHome);
		CleanupAvds();
	}

	void CleanupAvds()
	{
		foreach (var avdName in Sdk.AvdManager.ListAvds().Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)))
			DeleteAvdIfExists(avdName, "Cleanup");
	}

	void DeleteAvdIfExists(string avdName, string context)
	{
		try
		{
			Sdk.AvdManager.Delete(avdName);
		}
		catch (SdkToolFailedExitException ex)
		{
			OutputHelper.WriteLine($"{context} delete failed for '{avdName}'. ExitCode={ex.ExitCode} Message={ex.Message}");
			foreach (var line in ex.StdErr ?? Array.Empty<string>())
				OutputHelper.WriteLine($"stderr: {line}");
			foreach (var line in ex.StdOut ?? Array.Empty<string>())
				OutputHelper.WriteLine($"stdout: {line}");
		}
	}

	[Fact]
	public void ListAvdsOnlyContainsCreatedAvd()
	{
		try
		{
			// Create the emulator instance
			Sdk.AvdManager.Create(TestEmulatorName, AndroidSdkManagerFixture.TestAvdPackageId, "pixel", force: true);

			var avds = Sdk.AvdManager.ListAvds();

			Assert.Single(avds.Select(a => a.Name), TestEmulatorName);
		}
		finally
		{
			// Delete the emulator
			Sdk.AvdManager.Delete(TestEmulatorName);
		}
	}

	[Fact]
	public void ListAvdsIsEmptyWhenNoAvdsWereCreated()
	{
		var avds = Sdk.AvdManager.ListAvds();

		Assert.Empty(avds);
	}

	[Fact]
	public void CreateEmulator()
	{
		const string TestAvdName = "CreateEmulator";

		try
		{
			// Create the emulator
			Sdk.AvdManager.Create(TestAvdName, AndroidSdkManagerFixture.TestAvdPackageId, "pixel", force: true);

			// Assert that it exists
			var avds = Sdk.AvdManager.ListAvds();
			var avd = Assert.Single(avds);
			Assert.Equal(TestAvdName, avd.Name, ignoreCase: true);
		}
		finally
		{
			DeleteAvdIfExists(TestAvdName, nameof(CreateEmulator));
		}
	}

	[Fact]
	public void CreateAndDeleteEmulator()
	{
		const string TestAvdName = "CreateAndDeleteEmulator";

		// Create the emulator
		Sdk.AvdManager.Create(TestAvdName, AndroidSdkManagerFixture.TestAvdPackageId, "pixel", force: true);

		// Assert that it exists
		var avds = Sdk.AvdManager.ListAvds();
		var avd = Assert.Single(avds);
		Assert.Equal(TestAvdName, avd.Name, ignoreCase: true);

		// Delete the emulator
		Sdk.AvdManager.Delete(TestAvdName);
	}

	[Fact]
	public void CreateEmulatorWithAbi()
	{
		const string TestAvdName = "CreateAndDeleteEmulatorWithAbi";

		var abi = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
			? "google_apis/arm64-v8a"
			: "google_apis/x86_64";

		try
		{
			// Create the emulator
			var options = new AvdManager.AvdCreateOptions { Device = "pixel", Force = true, Abi = abi };
			Sdk.AvdManager.Create(TestAvdName, AndroidSdkManagerFixture.TestAvdPackageId, options);

			// Assert that it exists
			var avds = Sdk.AvdManager.ListAvds();
			var avd = Assert.Single(avds);
			Assert.Equal(TestAvdName, avd.Name, ignoreCase: true);
		}
		finally
		{
			DeleteAvdIfExists(TestAvdName, nameof(CreateEmulatorWithAbi));
		}
	}
	
	[Fact]
	public void LocatedPath()
	{
		var l = new AvdLocator();
		var p = l.Locate();

		Assert.NotNull(p);
		Assert.NotEmpty(p);
	}

	[Fact]
	public void LocatedPathForEnvVar()
	{
		var oldHome = avdEnvironment.AndroidAvdHome;

		var tempAvdPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AvdManager_Tests), nameof(LocatedPathForEnvVar), "android-avd-home");
		Directory.CreateDirectory(tempAvdPath);

		try
		{
			Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", tempAvdPath);

			var l = new AvdLocator();
			var p = l.Locate();

			Assert.NotNull(p);
			Assert.NotEmpty(p);

			Assert.Equal(tempAvdPath, p[0].FullName);
		}
		finally
		{
			Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", oldHome);
			Directory.Delete(tempAvdPath, true);
		}
	}
}
