#nullable enable
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

public class BootedEmulatorFixture : IDisposable
{
	readonly object gate = new();
	string? oldAndroidAvdHome;
	string? sharedAndroidAvdHome;
	AndroidSdkManager? sdk;
	IMessageSink? messageSink;
	bool initialized;

	public string AvdName { get; private set; } = string.Empty;

	public string? AndroidAvdHome => sharedAndroidAvdHome;

	public Emulator.AndroidEmulatorProcess EmulatorInstance { get; private set; } = null!;

	public void EnsureInitialized(AndroidSdkManagerFixture fixture)
	{
		lock (gate)
		{
			if (initialized)
				return;

			sdk = fixture.Sdk;
			messageSink = fixture.MessageSink;
			oldAndroidAvdHome = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");
			sharedAndroidAvdHome = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(BootedEmulatorFixture), "android-avd-home");
			AvdName = "TestBootedEmu" + Guid.NewGuid().ToString("N")[..6];

			if (Directory.Exists(sharedAndroidAvdHome))
				Directory.Delete(sharedAndroidAvdHome, recursive: true);

			Directory.CreateDirectory(sharedAndroidAvdHome);
			Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", sharedAndroidAvdHome);
			messageSink.OnMessage(new DiagnosticMessage($"Set ANDROID_AVD_HOME to {sharedAndroidAvdHome}"));

			try
			{
				sdk.AvdManager.Create(AvdName, AndroidSdkManagerFixture.TestAvdPackageId, "pixel", force: true);
				EmulatorInstance = sdk.Emulator.Start(AvdName, CreateHeadlessOptions(port: 5554));

				var booted = EmulatorInstance.WaitForBootComplete(TimeSpan.FromMinutes(15));
				Assert.True(booted);
				Assert.NotEmpty(EmulatorInstance.Serial);
				initialized = true;
			}
			catch (SdkToolFailedExitException ex)
			{
				LogSdkToolException("BootedEmulatorFixture.EnsureInitialized", ex);
				throw;
			}
			finally
			{
				Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", oldAndroidAvdHome);
			}
		}
	}

	public void Dispose()
	{
		lock (gate)
		{
			if (!initialized || sdk == null)
				return;

			try
			{
				var shutdown = EmulatorInstance.Shutdown();
				Assert.True(shutdown);
			}
			catch (SdkToolFailedExitException ex)
			{
				LogSdkToolException("BootedEmulatorFixture.Dispose shutdown", ex);
			}

			var currentAndroidAvdHome = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");
			try
			{
				if (!string.IsNullOrEmpty(sharedAndroidAvdHome))
					Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", sharedAndroidAvdHome);
				sdk.AvdManager.Delete(AvdName);
			}
			catch (SdkToolFailedExitException ex)
			{
				LogSdkToolException("BootedEmulatorFixture.Dispose delete", ex);
			}
			finally
			{
				Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", currentAndroidAvdHome);
				if (!string.IsNullOrEmpty(sharedAndroidAvdHome) && Directory.Exists(sharedAndroidAvdHome))
					Directory.Delete(sharedAndroidAvdHome, recursive: true);
			}
		}
	}

	static Emulator.EmulatorStartOptions CreateHeadlessOptions(uint? port = null, int? memoryMegabytes = null, int? partitionSizeMegabytes = null)
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

	void LogSdkToolException(string action, SdkToolFailedExitException ex)
	{
		messageSink?.OnMessage(new DiagnosticMessage($"{action} failed. ExitCode={ex.ExitCode} Message={ex.Message}"));

		if (ex.StdErr?.Length > 0)
		{
			messageSink?.OnMessage(new DiagnosticMessage($"{action} stderr:"));
			foreach (var line in ex.StdErr)
				messageSink?.OnMessage(new DiagnosticMessage(line));
		}

		if (ex.StdOut?.Length > 0)
		{
			messageSink?.OnMessage(new DiagnosticMessage($"{action} stdout:"));
			foreach (var line in ex.StdOut)
				messageSink?.OnMessage(new DiagnosticMessage(line));
		}
	}
}
