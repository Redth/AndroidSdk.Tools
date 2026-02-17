#nullable enable
using System;
using System.IO;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

/// <summary>
/// Per-test AVD home scope.
/// Ensures each test uses an isolated ANDROID_AVD_HOME and restores the previous value on cleanup.
/// </summary>
internal sealed class AvdHomeScope : IDisposable
{
	readonly IMessageSink? sink;
	readonly ITestOutputHelper? output;

	readonly string? previousAndroidAvdHome;
	readonly string tempAndroidAvdHome;

	public AvdHomeScope(IMessageSink messageSink, string? testId = null)
		: this(messageSink, null, testId)
	{
	}

	public AvdHomeScope(ITestOutputHelper outputHelper, string? testId = null)
		: this(null, outputHelper, testId)
	{
	}

	AvdHomeScope(IMessageSink? messageSink, ITestOutputHelper? outputHelper, string? testId = null)
	{
		sink = messageSink;
		output = outputHelper;

		previousAndroidAvdHome = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");
		Log($"Previous ANDROID_AVD_HOME: '{previousAndroidAvdHome}'");

		var tempRoot = Path.GetTempPath();
		if (!string.IsNullOrEmpty(previousAndroidAvdHome) && previousAndroidAvdHome.StartsWith(tempRoot, StringComparison.Ordinal))
			throw new InvalidOperationException($"ANDROID_AVD_HOME was not unset from a previous test scope: '{previousAndroidAvdHome}'.");

		if (string.IsNullOrWhiteSpace(testId))
			testId = Guid.NewGuid().ToString("N");

		tempAndroidAvdHome = Path.Combine(tempRoot, "AndroidSdk.Tests", nameof(AvdHomeScope), testId, "android-avd-home");

		if (Directory.Exists(tempAndroidAvdHome))
		{
			Directory.Delete(tempAndroidAvdHome, true);
			Log($"Deleted existing temporary AVD home directory '{tempAndroidAvdHome}'");
		}

		Directory.CreateDirectory(tempAndroidAvdHome);
		Log($"Created temporary AVD home directory '{tempAndroidAvdHome}'");

		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", tempAndroidAvdHome);
		Log($"Set ANDROID_AVD_HOME to '{tempAndroidAvdHome}'");
	}

	public string AndroidAvdHome => tempAndroidAvdHome;

	public void Dispose()
	{
		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", previousAndroidAvdHome);
		Log($"Restored ANDROID_AVD_HOME to '{previousAndroidAvdHome}'");

		try
		{
			if (Directory.Exists(tempAndroidAvdHome))
			{
				Directory.Delete(tempAndroidAvdHome, true);
				Log($"Deleted temporary AVD home directory '{tempAndroidAvdHome}'");
			}
		}
		catch (IOException ex)
		{
			Log($"Failed to delete temporary AVD home directory '{tempAndroidAvdHome}': {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			Log($"Failed to delete temporary AVD home directory '{tempAndroidAvdHome}': {ex.Message}");
		}
	}

	void Log(string message)
	{
		sink?.OnMessage(new DiagnosticMessage(message));
		output?.WriteLine(message);
	}
}
