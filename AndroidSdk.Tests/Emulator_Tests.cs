using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

[Collection(AndroidSdkManagerCollection.Name)]
public class Emulator_Tests : EmulatorTestsBase, IClassFixture<AvdEnvironmentFixture>
{
	readonly AvdEnvironmentFixture avdEnvironment;

	public Emulator_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture, AvdEnvironmentFixture avdEnvironment)
		: base(outputHelper, fixture)
	{
		this.avdEnvironment = avdEnvironment;
		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", avdEnvironment.AndroidAvdHome);
	}

	string CreateTestAvd()
	{
		var avdName = "TestEmu" + Guid.NewGuid().ToString("N")[..6];
		Sdk.AvdManager.Create(avdName, AndroidSdkManagerFixture.TestAvdPackageId, "pixel", force: true);
		return avdName;
	}

	void DeleteTestAvd(string avdName)
	{
		try
		{
			Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", avdEnvironment.AndroidAvdHome);
			Sdk.AvdManager.Delete(avdName);
		}
		catch (SdkToolFailedExitException ex)
		{
			OutputHelper.WriteLine($"DeleteTestAvd({avdName}) failed. ExitCode={ex.ExitCode} Message={ex.Message}");
			foreach (var line in ex.StdErr ?? Array.Empty<string>())
				OutputHelper.WriteLine($"stderr: {line}");
			foreach (var line in ex.StdOut ?? Array.Empty<string>())
				OutputHelper.WriteLine($"stdout: {line}");
		}
	}

	[Fact]
	public void ListAvdsOnlyContainsCreatedAvd()
	{
		var avdName = CreateTestAvd();
		try
		{
			var avds = Sdk.Emulator.ListAvds().ToList();

			for (var i = avds.Count - 1; i >= 0; i--)
			{
				if (avds[i].StartsWith("INFO "))
					avds.RemoveAt(i);
			}

			Assert.Single(avds, avdName);
		}
		finally
		{
			DeleteTestAvd(avdName);
		}
	}

	[Fact]
	public void CreateAndStartAndStopEmulator()
	{
		var avdName = CreateTestAvd();
		try
		{
			var options = CreateHeadlessOptions();
			var emulatorInstance = Sdk.Emulator.Start(avdName, options);

			var output = emulatorInstance.GetOutput().ToList();
			foreach (var line in output)
				OutputHelper.WriteLine(line);

			var booted = emulatorInstance.WaitForBootComplete(TimeSpan.FromMinutes(15));

			var output2 = emulatorInstance.GetOutput().Skip(output.Count).ToList();
			foreach (var line in output2)
				OutputHelper.WriteLine(line);

			Assert.True(booted);
			Assert.NotEmpty(emulatorInstance.Serial);
			Assert.Equal(avdName, emulatorInstance.AvdName);

			var shutdown = emulatorInstance.Shutdown();
			Assert.True(shutdown);
		}
		finally
		{
			DeleteTestAvd(avdName);
		}
	}

	[Fact]
	public void CreateAndStartAndStopHeadlessEmulatorWithOptions()
	{
		var avdName = CreateTestAvd();
		try
		{
			const uint port = 5564;
			var options = CreateHeadlessOptions(port: port, memoryMegabytes: 2048, partitionSizeMegabytes: 4096);
			var emulatorInstance = Sdk.Emulator.Start(avdName, options);

			var output = emulatorInstance.GetOutput().ToList();
			foreach (var line in output)
				OutputHelper.WriteLine(line);

			var booted = emulatorInstance.WaitForBootComplete(TimeSpan.FromMinutes(15));

			var output2 = emulatorInstance.GetOutput().Skip(output.Count).ToList();
			foreach (var line in output2)
				OutputHelper.WriteLine(line);

			Assert.True(booted);
			Assert.Equal($"emulator-{port}", emulatorInstance.Serial);
			Assert.Equal(avdName, emulatorInstance.AvdName);

			var shutdown = emulatorInstance.Shutdown();
			Assert.True(shutdown);
		}
		finally
		{
			DeleteTestAvd(avdName);
		}
	}
}
