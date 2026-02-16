using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// AVD manager integration tests.
/// Validates create/list/delete and locator-adjacent behaviors while isolating AVD home state per test.
/// </summary>
public class AvdManager_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
	: AvdManagerTestsBase(outputHelper, fixture), IClassFixture<AvdManager_Tests.OneTimeSetup>
{
	static readonly string TestAvdPackageId =
		RuntimeInformation.ProcessArchitecture == Architecture.Arm64
			? "system-images;android-30;google_apis;arm64-v8a"
			: "system-images;android-30;google_apis;x86_64";

	readonly string TestEmulatorName = "TestAvd" + Guid.NewGuid().ToString("N").Substring(0, 6);
	readonly AvdHomeScope avdHomeScope = new($"{nameof(AvdManager_Tests)}.{Guid.NewGuid():N}");

	/// <summary>
	/// One-time package setup.
	/// Installs the required system image once so AVD creation tests can run without repeated package installation.
	/// </summary>
	private class OneTimeSetup
	{
		public OneTimeSetup(AndroidSdkManagerFixture fixture)
		{
			var sdk = fixture.Sdk;

			// Install
			var ok = sdk.SdkManager.Install(TestAvdPackageId);
			Assert.True(ok);

			// Assert that it installed
			var list = sdk.SdkManager.List();
			Assert.Contains(TestAvdPackageId, list.InstalledPackages.Select(p => p.Path));
		}
	}

	public override void Dispose()
	{
		try
		{
			// Delete the emulator
			Sdk.AvdManager.Delete(TestEmulatorName);
		}
		catch
		{
			// The emulator may have failed to be created, so ignore any exceptions from delete attempts in Dispose.
		}

		avdHomeScope.Dispose();

		base.Dispose();
	}
	
	[Fact]
	public void LocatedPathIsExpectedPath()
	{
		var l = new AvdLocator();
		var p = l.Locate();

		Assert.NotNull(p);
		Assert.NotEmpty(p);
		Assert.Equal(avdHomeScope.AndroidAvdHome, p[0].FullName);
	}

	[Fact]
	public void ListAvdsIsEmptyWhenNoAvdsWereCreated()
	{
		var avds = Sdk.AvdManager.ListAvds();

		Assert.Empty(avds);
	}

	[Fact]
	public void ListAvdsOnlyContainsCreatedAvd()
	{
		// Create the emulator instance
		Sdk.AvdManager.Create(TestEmulatorName, TestAvdPackageId, "pixel", force: true);

		// Assert that it exists
		var avds = Sdk.AvdManager.ListAvds();
		var avd = Assert.Single(avds);
		Assert.Equal(TestEmulatorName, avd.Name, ignoreCase: true);
	}

	[Fact]
	public void ListAvdsOnlyContainsCreatedAvdWithAbi()
	{
		var abi = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
			? "google_apis/arm64-v8a"
			: "google_apis/x86_64";

		// Create the emulator
		var options = new AvdManager.AvdCreateOptions { Device = "pixel", Force = true, Abi = abi };
		Sdk.AvdManager.Create(TestEmulatorName, TestAvdPackageId, options);

		// Assert that it exists
		var avds = Sdk.AvdManager.ListAvds();
		var avd = Assert.Single(avds);
		Assert.Equal(TestEmulatorName, avd.Name, ignoreCase: true);
	}

	[Fact]
	public void CreateAndDeleteEmulator()
	{
		// Create the emulator
		Sdk.AvdManager.Create(TestEmulatorName, TestAvdPackageId, "pixel", force: true);

		// Assert that it exists
		var avds = Sdk.AvdManager.ListAvds();
		var avd = Assert.Single(avds);
		Assert.Equal(TestEmulatorName, avd.Name, ignoreCase: true);

		// Delete the emulator
		Sdk.AvdManager.Delete(TestEmulatorName);

		// Assert that it no longer exists
		avds = Sdk.AvdManager.ListAvds();
		Assert.Empty(avds);
	}
}
