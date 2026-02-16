#nullable enable
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

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

	public class OneTimeSetup : IDisposable
	{
		readonly AndroidSdkManager sdk;
		readonly IMessageSink sink;

		public OneTimeSetup(IMessageSink messageSink, AndroidSdkManagerFixture fixture)
		{
			sdk = fixture.Sdk;
			sink = messageSink;

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
	}
}
