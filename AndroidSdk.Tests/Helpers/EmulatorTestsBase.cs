#nullable enable
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public abstract class EmulatorTestsBase(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
	: AvdManagerTestsBase(outputHelper, fixture), IClassFixture<EmulatorTestsBase.OneTimeSetup>
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
			NoWindow = true,
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

		public OneTimeSetup(AndroidSdkManagerFixture fixture)
		{
			sdk = fixture.Sdk;

			var installOk = sdk.SdkManager.Install(TestAvdPackageId);
			Assert.True(installOk);

			var list = sdk.SdkManager.List();
			Assert.Contains(TestAvdPackageId, list.InstalledPackages.Select(p => p.Path));

			sdk.AvdManager.Create(TestEmulatorName, TestAvdPackageId, "pixel", force: true);
			Assert.Contains(sdk.AvdManager.ListAvds(), a => a.Name == TestEmulatorName);
		}

		public void Dispose()
		{
			sdk.AvdManager.Delete(TestEmulatorName);

			var uninstallOk = sdk.SdkManager.Uninstall(TestAvdPackageId);
			Assert.True(uninstallOk);

			var list = sdk.SdkManager.List();
			Assert.DoesNotContain(TestAvdPackageId, list.InstalledPackages.Select(p => p.Path));
		}
	}
}
