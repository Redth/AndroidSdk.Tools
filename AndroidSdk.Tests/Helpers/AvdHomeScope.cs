#nullable enable
using System;
using System.IO;

namespace AndroidSdk.Tests;

/// <summary>
/// Per-test AVD home scope.
/// Ensures each test uses an isolated ANDROID_AVD_HOME and restores the previous value on cleanup.
/// </summary>
public sealed class AvdHomeScope : IDisposable
{
	readonly string? previousAndroidAvdHome;
	readonly string tempAndroidAvdHome;

	public AvdHomeScope(string? testId = null)
	{
		previousAndroidAvdHome = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");

		var tempRoot = Path.GetTempPath();
		if (!string.IsNullOrEmpty(previousAndroidAvdHome) && previousAndroidAvdHome.StartsWith(tempRoot, StringComparison.Ordinal))
			throw new InvalidOperationException("ANDROID_AVD_HOME was not unset from a previous test scope.");

		if (string.IsNullOrWhiteSpace(testId))
			testId = Guid.NewGuid().ToString("N");

		tempAndroidAvdHome = Path.Combine(tempRoot, "AndroidSdk.Tests", nameof(AvdHomeScope), testId, "android-avd-home");

		if (Directory.Exists(tempAndroidAvdHome))
			Directory.Delete(tempAndroidAvdHome, true);
		Directory.CreateDirectory(tempAndroidAvdHome);

		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", tempAndroidAvdHome);
	}

	public string AndroidAvdHome => tempAndroidAvdHome;

	public void Dispose()
	{
		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", previousAndroidAvdHome);

		if (Directory.Exists(tempAndroidAvdHome))
			Directory.Delete(tempAndroidAvdHome, true);
	}
}
