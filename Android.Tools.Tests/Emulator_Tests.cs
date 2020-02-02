using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Android.Tools.Tests
{
	public class Emulator_Tests : TestsBase
	{
		public Emulator_Tests(ITestOutputHelper outputHelper)
			: base(outputHelper)
		{ }

		[Fact]
		public void CreateAndStartAndStopEmulator()
		{
			var sdk = GetSdk();
			sdk.Acquire();

			var avdImagePackageId = "system-images;android-29;google_apis_playstore;x86_64";

			// Install the right avd image
			sdk.SdkManager.Install(avdImagePackageId);

			sdk.AvdManager.Create("TestEmu123", avdImagePackageId, "pixel", force: true);

			var emulatorInstance = sdk.Emulator.Start("TestEmu123");

			emulatorInstance.WaitForBootComplete();

			emulatorInstance.Shutdown();
		}
	}
}
