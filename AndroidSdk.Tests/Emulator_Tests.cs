using System;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests
{
	public class Emulator_Tests : TestsBase
	{
		const string TestEmulatorName = "TestEmu123";
		const string TestAvdImagePackageId = "system-images;android-31;google_apis;x86_64";

		public Emulator_Tests(ITestOutputHelper outputHelper)
			: base(outputHelper)
		{ }

		[Fact]
		public Task CreateAndStartAndStopEmulator() =>
			CreateTestEmulator(sdk =>
			{
				// Start the emulator
				var emulatorInstance = sdk.Emulator.Start(TestEmulatorName);

				// Wait for the boot
				var booted = emulatorInstance.WaitForBootComplete();
				Assert.True(booted);

				// Assert that the emulator is valid
				Assert.NotEmpty(emulatorInstance.Serial);
				Assert.Equal(TestEmulatorName, emulatorInstance.AvdName);

				// Shutdown the emulator
				var shutdown = emulatorInstance.Shutdown();
				Assert.True(shutdown);
			});

		[Fact]
		public Task CreateAndStartAndStopHeadlessEmulatorWithOptions() =>
			CreateTestEmulator(sdk =>
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
				var emulatorInstance = sdk.Emulator.Start(TestEmulatorName, options);

				// Wait for the boot
				var booted = emulatorInstance.WaitForBootComplete();
				Assert.True(booted);

				// Assert that the emulator is valid
				Assert.Equal("emulator-5554", emulatorInstance.Serial);
				Assert.Equal(TestEmulatorName, emulatorInstance.AvdName);

				// Shutdown the emulator
				var shutdown = emulatorInstance.Shutdown();
				Assert.True(shutdown);
			});

		[Fact]
		public async Task ListAvds()
		{
			var sdk = GetSdk();
			await sdk.Acquire();

			var avds = sdk.Emulator.ListAvds();

			Assert.NotNull(avds);
		}

		Task CreateTestEmulator(Action<AndroidSdkManager> test) =>
			CreateTestEmulator(sdk =>
			{
				test(sdk);
				return Task.CompletedTask;
			});

		async Task CreateTestEmulator(Func<AndroidSdkManager, Task> test)
		{
			var sdk = GetSdk();
			await sdk.Acquire();

			// Install the right avd image
			sdk.SdkManager.Install(TestAvdImagePackageId);

			// Create the emulator instance
			sdk.AvdManager.Create(TestEmulatorName, TestAvdImagePackageId, "pixel", force: true);

			// Run the actual test
			await test(sdk);

			// Delete the emulator
			sdk.AvdManager.Delete(TestEmulatorName);
		}
	}
}
