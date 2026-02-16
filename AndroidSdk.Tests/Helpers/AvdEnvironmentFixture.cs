#nullable enable
using System;
using System.IO;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

public class AvdEnvironmentFixture : IDisposable
{
	readonly IMessageSink? messageSink;
	readonly string? oldAndroidAvdHome;

	public AvdEnvironmentFixture(IMessageSink sink)
	{
		messageSink = sink;
		oldAndroidAvdHome = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");
		AndroidAvdHome = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AvdEnvironmentFixture), Guid.NewGuid().ToString("N"), "android-avd-home");

		Directory.CreateDirectory(AndroidAvdHome);
		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", AndroidAvdHome);
		messageSink.OnMessage(new DiagnosticMessage($"Set ANDROID_AVD_HOME to {AndroidAvdHome}"));
	}

	public string AndroidAvdHome { get; }

	public void Dispose()
	{
		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", oldAndroidAvdHome);

		try
		{
			if (Directory.Exists(AndroidAvdHome))
				Directory.Delete(AndroidAvdHome, recursive: true);
		}
		catch (IOException ex)
		{
			messageSink?.OnMessage(new DiagnosticMessage($"Failed to delete temporary AVD home '{AndroidAvdHome}': {ex.Message}"));
		}
		catch (UnauthorizedAccessException ex)
		{
			messageSink?.OnMessage(new DiagnosticMessage($"Failed to delete temporary AVD home '{AndroidAvdHome}': {ex.Message}"));
		}
	}
}
