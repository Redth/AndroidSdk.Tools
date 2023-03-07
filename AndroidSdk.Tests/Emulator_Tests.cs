using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests
{
	public class Emulator_Tests : TestsBase
	{
		public Emulator_Tests(ITestOutputHelper outputHelper)
			: base(outputHelper)
		{ }

		//[Fact]
		public void CreateAndStartAndStopEmulator()
		{
			var sdk = GetSdk();
			sdk.Acquire();

			var avdImagePackageId = "system-images;android-31;google_apis;x86_64";

			// Install the right avd image
			sdk.SdkManager.Install(avdImagePackageId);

			sdk.AvdManager.Create("TestEmu123", avdImagePackageId, "pixel", force: true);

			var emulatorInstance = sdk.Emulator.Start("TestEmu123");

			var booted = emulatorInstance.WaitForBootComplete();

			Assert.True(booted);

			var shutdown = emulatorInstance.Shutdown();

			Assert.True(shutdown);
		}

		[Fact]
		public async Task CreateEmulator()
		{
			var sdk = GetSdk();
			await sdk.Acquire();

			var avdImagePackageId = "system-images;android-30;google_apis;x86_64";

			// Install the right avd image
			sdk.SdkManager.Install(avdImagePackageId);

			sdk.AvdManager.Create("TestEmu123", avdImagePackageId, "pixel", force: true);


			var avds = sdk.AvdManager.ListAvds();

			Assert.Contains(avds, avd => avd.Name.Equals("TestEmu123", StringComparison.OrdinalIgnoreCase));
		}

		[Fact]
		public async Task ListAvds()
		{
			var sdk = GetSdk();
			await sdk.Acquire();

			var avds = sdk.Emulator.ListAvds();

			Assert.NotNull(avds);
		}
	}
}
