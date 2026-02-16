using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// Emulator lifecycle tests that reuse a one-time prepared AVD/image fixture.
/// </summary>
public class Emulator_Tests : EmulatorTestsBase, IClassFixture<EmulatorTestsBase.AvdCreateFixture>
{
	public Emulator_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture, AvdCreateFixture setup)
		: base(outputHelper, fixture)
	{
		_ = setup;
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
		var options = CreateHeadlessOptions();
		var emulatorInstance = Sdk.Emulator.Start(TestEmulatorName, options);

		// Write output so far
		var output = emulatorInstance.GetOutput().ToList();
		foreach (var line in output)
			OutputHelper.WriteLine(line);

		// Wait for the boot
		var booted = emulatorInstance.WaitForBootComplete(TimeSpan.FromMinutes(15));

		// Write the rest
		var output2 = emulatorInstance.GetOutput().Skip(output.Count).ToList();
		foreach (var line in output2)
			OutputHelper.WriteLine(line);

		// Make sure it booted
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
		var options = CreateHeadlessOptions(port: 5554, memoryMegabytes: 2048, partitionSizeMegabytes: 4096);
		var emulatorInstance = Sdk.Emulator.Start(TestEmulatorName, options);

		// Write output so far
		var output = emulatorInstance.GetOutput().ToList();
		foreach (var line in output)
			OutputHelper.WriteLine(line);

		// Wait for the boot
		var booted = emulatorInstance.WaitForBootComplete(TimeSpan.FromMinutes(15));

		// Write the rest
		var output2 = emulatorInstance.GetOutput().Skip(output.Count).ToList();
		foreach (var line in output2)
			OutputHelper.WriteLine(line);

		// Make sure it booted
		Assert.True(booted);

		// Assert that the emulator is valid
		Assert.Equal("emulator-5554", emulatorInstance.Serial);
		Assert.Equal(TestEmulatorName, emulatorInstance.AvdName);

		// Shutdown the emulator
		var shutdown = emulatorInstance.Shutdown();
		Assert.True(shutdown);
	}
}
