using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests
{
	public class AvdManager_Tests : TestsBase
	{
		const string TestAvdName = "TestAvd123";
		const string TestAvdPackageId = "system-images;android-30;google_apis;x86_64";

		public AvdManager_Tests(ITestOutputHelper outputHelper)
			: base(outputHelper)
		{ }

		[Fact]
		public void LocatedPath()
		{
			var l = new AvdLocator();
			var p = l.Locate();

			Assert.NotNull(p);
		}

		[Fact]
		public void ListAvds()
		{
			var sdk = GetSdk();
			var avds = sdk.AvdManager.ListAvds();

			Assert.NotEmpty(avds);
		}

		[Fact]
		public async Task InstallEmulator()
		{
			var sdk = GetSdk();
			await sdk.Acquire();

			// Install
			var ok = sdk.SdkManager.Install(TestAvdPackageId);
			Assert.True(ok);

			// Assert that it installed
			var list = sdk.SdkManager.List();
			Assert.Contains(TestAvdPackageId, list.InstalledPackages.Select(p => p.Path));
		}

		[Fact]
		public async Task CreateAndDeleteEmulator()
		{
			var sdk = GetSdk();
			await sdk.Acquire();

			// Install the right avd image
			sdk.SdkManager.Install(TestAvdPackageId);

			// Create the emulator
			sdk.AvdManager.Create(TestAvdName, TestAvdPackageId, "pixel", force: true);

			// Assert that it exists
			var avds = sdk.AvdManager.ListAvds();
			Assert.Contains(avds, avd => avd.Name.Equals(TestAvdName, StringComparison.OrdinalIgnoreCase));

			// Delete the emulator
			sdk.AvdManager.Delete(TestAvdName);
		}

		[Fact]
		public async Task CreateAndDeleteEmulatorWithAbi()
		{
			var sdk = GetSdk();
			await sdk.Acquire();

			// Install the right avd image
			sdk.SdkManager.Install(TestAvdPackageId);

			// Create the emulator
			var options = new AvdManager.AvdCreateOptions { Device = "pixel", Force = true, Abi = "google_apis/x86_64" };
			sdk.AvdManager.Create(TestAvdName, TestAvdPackageId, options);

			// Assert that it exists
			var avds = sdk.AvdManager.ListAvds();
			Assert.Contains(avds, avd => avd.Name.Equals(TestAvdName, StringComparison.OrdinalIgnoreCase));

			// Delete the emulator
			sdk.AvdManager.Delete(TestAvdName);
		}
	}
}
