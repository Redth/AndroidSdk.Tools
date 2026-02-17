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
	: AndroidSdkManagerTestsBase(outputHelper, fixture)
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
			// Always headless on CI, but allow non-CI to specify if they want headless or not
			NoWindow = IsCI,
			// Use the right GPU on Linux
			Gpu = OperatingSystem.IsLinux()
				? "swiftshader_indirect"
				: "guest",
			// Don't use snapshots on CI to better test cold boot and avoid snapshot corruption issues on CI agents
			NoSnapshot = IsCI,
			NoAudio = true,
			NoBootAnim = true,
			MemoryMegabytes = memoryMegabytes,
			PartitionSizeMegabytes = partitionSizeMegabytes,
		};

	/// <summary>
	/// One-time emulator class setup fixture.
	/// Installs the emulator image and creates a reusable AVD inside an isolated fixture-scoped AVD home.
	/// </summary>
	public class AvdCreateFixture : IDisposable
	{
		readonly AndroidSdkManager sdk;
		readonly IMessageSink sink;
		readonly AvdHomeScope avdHomeScope;

		// Only uninstall the system image if we're running in CI, to avoid uninstalling a potentially
		// shared global SDK's system image on a developer machine that may be reused across test runs.
		// The AVD home isolation ensures that the created AVD won't be visible to other test runs even
		// if we don't uninstall the system image, so it should be safe to leave the system image
		// installed on a developer machine for reuse across test runs.
		readonly bool UninstallPackages = IsCI;

		public AvdCreateFixture(IMessageSink messageSink, AndroidSdkManagerFixture fixture)
		{
			sdk = fixture.Sdk;
			sink = messageSink;
			avdHomeScope = new AvdHomeScope(messageSink, $"{nameof(EmulatorTestsBase)}.{nameof(AvdCreateFixture)}.{Guid.NewGuid():N}");

			sink.OnMessage(new DiagnosticMessage($"Checking if system image {TestAvdPackageId} is installed..."));
			var installedList = sdk.SdkManager.List();
			if (!installedList.InstalledPackages.Any(p => p.Path == TestAvdPackageId))
			{
				sink.OnMessage(new DiagnosticMessage($"Installing system image {TestAvdPackageId} for emulator tests..."));
				var installOk = sdk.SdkManager.Install(TestAvdPackageId);
				Assert.True(installOk);
				sink.OnMessage(new DiagnosticMessage("Installed system image."));

				sink.OnMessage(new DiagnosticMessage("Asserting system image is installed..."));
				var list = sdk.SdkManager.List();
				Assert.Contains(TestAvdPackageId, list.InstalledPackages.Select(p => p.Path));
				sink.OnMessage(new DiagnosticMessage("Asserted system image is installed."));
			}
			else
			{
				sink.OnMessage(new DiagnosticMessage("System image already installed. Skipping download."));
			}

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

				if (UninstallPackages)
				{
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
			finally
			{
				avdHomeScope.Dispose();
			}
		}
	}
}
