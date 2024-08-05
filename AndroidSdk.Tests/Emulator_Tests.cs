using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class Emulator_Tests : AvdManagerTestsBase, IClassFixture<Emulator_Tests.OneTimeSetup>, IDisposable
{
	static readonly string TestEmulatorName = "TestEmu" + Guid.NewGuid().ToString("N").Substring(0, 6);
	static readonly string TestAvdPackageId = "system-images;android-31;google_apis;x86_64";

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

	public Emulator_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
		: base(outputHelper, fixture)
	{
		// Create the emulator instance
		Sdk.AvdManager.Create(TestEmulatorName, TestAvdPackageId, "pixel", force: true);
	}

	public override void Dispose()
	{
		// Delete the emulator
		Sdk.AvdManager.Delete(TestEmulatorName);

		base.Dispose();
	}

	[Fact]
	public void ListAvdsOnlyContainsCreatedAvd()
	{
		var avds = Sdk.Emulator.ListAvds().ToList();

		// TODO: remove the debug info items
		for (var i = avds.Count - 1; i >= 0; i--)
		{
			if (avds[i].StartsWith("INFO "))
				avds.RemoveAt(i);
		}

		Assert.Single(avds, TestEmulatorName);
	}

	[Fact]
	public void CreateAndStartAndStopEmulator()
	{
		// Start the emulator
		var emulatorInstance = Sdk.Emulator.Start(TestEmulatorName);

		// Wait for the boot
		var booted = emulatorInstance.WaitForBootComplete(TimeSpan.FromMinutes(10));
		Assert.True(booted);

		// Assert that the emulator is valid
		Assert.NotEmpty(emulatorInstance.Serial);
		Assert.Equal(TestEmulatorName, emulatorInstance.AvdName);

		// Shutdown the emulator
		var shutdown = emulatorInstance.Shutdown();
		Assert.True(shutdown);
	}

	[Fact]
	public void CreateAndStartAndStopHeadlessEmulatorWithOptions()
	{
		// Start the emulator
		var options = new Emulator.EmulatorStartOptions
		{
			Port = 5554,
			NoWindow = true,
			Gpu = "swiftshader_indirect",
			NoSnapshot = true,
			NoAudio = true,
			NoBootAnim = true
		};
		var emulatorInstance = Sdk.Emulator.Start(TestEmulatorName, options);

		// Wait for the boot
		var booted = emulatorInstance.WaitForBootComplete(TimeSpan.FromMinutes(10));
		Assert.True(booted);

		// Assert that the emulator is valid
		Assert.Equal("emulator-5554", emulatorInstance.Serial);
		Assert.Equal(TestEmulatorName, emulatorInstance.AvdName);

		// Shutdown the emulator
		var shutdown = emulatorInstance.Shutdown();
		Assert.True(shutdown);
	}
}
