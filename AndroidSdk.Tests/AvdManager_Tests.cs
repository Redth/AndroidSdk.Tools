using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class AvdManager_Tests : AvdManagerTestsBase, IClassFixture<AvdManager_Tests.OneTimeSetup>
{
	static readonly string TestEmulatorName = "TestAvd" + Guid.NewGuid().ToString("N").Substring(0, 6);
	static readonly string TestAvdPackageId =
		RuntimeInformation.ProcessArchitecture == Architecture.Arm64
			? "system-images;android-30;google_apis;arm64-v8a"
			: "system-images;android-30;google_apis;x86_64";

	// Make sure the emulator is installed, but only do this once for all
	// the tests in this class to make things a fair bit faster.
	public class OneTimeSetup
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

	public AvdManager_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
		: base(outputHelper, fixture)
	{
	}

	[Fact]
	public void ListAvdsOnlyContainsCreatedAvd()
	{
		try
		{
			// Create the emulator instance
			Sdk.AvdManager.Create(TestEmulatorName, TestAvdPackageId, "pixel", force: true);

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

		// Create the emulator
		Sdk.AvdManager.Create(TestAvdName, TestAvdPackageId, "pixel", force: true);

		// Assert that it exists
		var avds = Sdk.AvdManager.ListAvds();
		var avd = Assert.Single(avds);
		Assert.Equal(TestAvdName, avd.Name, ignoreCase: true);
	}

	[Fact]
	public void CreateAndDeleteEmulator()
	{
		const string TestAvdName = "CreateAndDeleteEmulator";

		// Create the emulator
		Sdk.AvdManager.Create(TestAvdName, TestAvdPackageId, "pixel", force: true);

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

		// Create the emulator
		var options = new AvdManager.AvdCreateOptions { Device = "pixel", Force = true, Abi = "google_apis/x86_64" };
		Sdk.AvdManager.Create(TestAvdName, TestAvdPackageId, options);

		// Assert that it exists
		var avds = Sdk.AvdManager.ListAvds();
		var avd = Assert.Single(avds);
		Assert.Equal(TestAvdName, avd.Name, ignoreCase: true);
	}
}
