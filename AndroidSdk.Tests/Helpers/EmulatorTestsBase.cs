#nullable enable
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

/// <summary>
/// Emulator integration test base class.
/// Centralizes shared emulator package/options behavior while fixture-level setup owns its own AVD home scope.
/// </summary>
public abstract class EmulatorTestsBase(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
	: AvdManagerTestsBase(outputHelper, fixture)
{
	protected static readonly string TestEmulatorName =
		string.Concat("TestEmu", Guid.NewGuid().ToString("N").AsSpan(0, 6));

	protected static readonly string TestAvdPackageId =
		RuntimeInformation.ProcessArchitecture == Architecture.Arm64
			? "system-images;android-31;google_apis;arm64-v8a"
			: "system-images;android-31;google_apis;x86_64";

	protected static Emulator.EmulatorStartOptions CreateHeadlessOptions(uint? port = null, int? memoryMegabytes = null, int? partitionSizeMegabytes = null)
		=> new()
		{
			Port = port,
			NoWindow = IsCI, // Always headless on CI, but allow non-CI to specify if they want headless or not
			Gpu = "swiftshader_indirect",
			NoSnapshot = true,
			NoAudio = true,
			NoBootAnim = true,
			MemoryMegabytes = memoryMegabytes,
			PartitionSizeMegabytes = partitionSizeMegabytes,
		};

	/// <summary>
	/// One-time emulator class setup fixture.
	/// Installs the emulator image and creates a reusable AVD inside an isolated fixture-scoped AVD home.
	/// </summary>
	public class AvdInstallFixture : IDisposable
	{
		readonly AndroidSdkManager sdk;
		readonly IMessageSink sink;
		readonly AvdHomeScope avdHomeScope;

		public AvdInstallFixture(IMessageSink messageSink, AndroidSdkManagerFixture fixture)
		{
			sdk = fixture.Sdk;
			sink = messageSink;
			avdHomeScope = new AvdHomeScope($"{nameof(EmulatorTestsBase)}.{nameof(AvdInstallFixture)}.{Guid.NewGuid():N}");

			sink.OnMessage(new DiagnosticMessage($"Installing system image {TestAvdPackageId} for emulator tests..."));
			var installOk = sdk.SdkManager.Install(TestAvdPackageId);
			Assert.True(installOk);
			sink.OnMessage(new DiagnosticMessage("Installed system image."));

			sink.OnMessage(new DiagnosticMessage("Asserting system image is installed..."));
			var list = sdk.SdkManager.List();
			Assert.Contains(TestAvdPackageId, list.InstalledPackages.Select(p => p.Path));
			sink.OnMessage(new DiagnosticMessage("Asserted system image is installed."));

			sink.OnMessage(new DiagnosticMessage($"Creating AVD {TestEmulatorName} for emulator tests..."));
			sdk.AvdManager.Create(TestEmulatorName, TestAvdPackageId, "pixel", force: true);
			Assert.Contains(sdk.AvdManager.ListAvds(), a => a.Name == TestEmulatorName);
			sink.OnMessage(new DiagnosticMessage("Created AVD."));
		}

		public virtual void Dispose()
		{
			try
			{
				sink.OnMessage(new DiagnosticMessage($"Deleting AVD {TestEmulatorName}..."));
				sdk.AvdManager.Delete(TestEmulatorName);
				sink.OnMessage(new DiagnosticMessage("Deleted AVD."));

				sink.OnMessage(new DiagnosticMessage($"Uninstalling system image {TestAvdPackageId}..."));
				var uninstallOk = sdk.SdkManager.Uninstall(TestAvdPackageId);
				Assert.True(uninstallOk);
				sink.OnMessage(new DiagnosticMessage("Uninstalled system image."));

				sink.OnMessage(new DiagnosticMessage("Asserting system image is uninstalled..."));
				var list = sdk.SdkManager.List();
				Assert.DoesNotContain(TestAvdPackageId, list.InstalledPackages.Select(p => p.Path));
				sink.OnMessage(new DiagnosticMessage("Asserted system image is uninstalled."));
			}
			finally
			{
				avdHomeScope.Dispose();
			}
		}
	}

    /// <summary>
    /// One-time boot fixture that composes shared setup, boots the emulator, and tears down in reverse order.
    /// </summary>
    public class EmulatorBootFixture : IDisposable
    {
		readonly IMessageSink sink;
		readonly AvdInstallFixture setup;
		readonly AndroidSdkManager sdk;

        public EmulatorBootFixture(IMessageSink messageSink, AndroidSdkManagerFixture fixture)
        {
            sink = messageSink;
            setup = new AvdInstallFixture(messageSink, fixture);
            sdk = fixture.Sdk;

            sink.OnMessage(new DiagnosticMessage("Starting emulator for tests that require a booted emulator..."));
            EmulatorInstance = sdk.Emulator.Start(TestEmulatorName, CreateHeadlessOptions(port: 5554));
            sink.OnMessage(new DiagnosticMessage("Started emulator."));

            sink.OnMessage(new DiagnosticMessage("Waiting for emulator to complete booting..."));
            var booted = EmulatorInstance.WaitForBootComplete(TimeSpan.FromMinutes(15));
            sink.OnMessage(new DiagnosticMessage("Emulator boot complete."));

            Assert.True(booted);
            Assert.NotEmpty(EmulatorInstance.Serial);
        }

        public Emulator.AndroidEmulatorProcess EmulatorInstance { get; }

        public void Dispose()
        {
			try
			{
				sink.OnMessage(new DiagnosticMessage("Shutting down emulator..."));
				var shutdown = EmulatorInstance.Shutdown();
				Assert.True(shutdown);
				sink.OnMessage(new DiagnosticMessage("Shut down emulator."));
			}
			finally
			{
				setup.Dispose();
			}
        }
    }
}
