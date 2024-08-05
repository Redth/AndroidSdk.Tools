using System;
using System.IO;
using System.Threading;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// A base class for all tests that will be working with AVDs and want to ensure the
/// AVD environemnt is clean and free of any previous AVDs.
/// </summary>
public abstract class AvdManagerTestsBase : AndroidSdkManagerTestsBase, IDisposable
{
	readonly string oldAndroidAvdHome;
	readonly string tempAndroidAvdHome;

	public AvdManagerTestsBase(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
		: base(outputHelper, fixture)
	{
		oldAndroidAvdHome = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");

		tempAndroidAvdHome = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", GetType().Name, "android-avd-home");
		RecreateDir(tempAndroidAvdHome);

		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", tempAndroidAvdHome);
	}

	public virtual void Dispose()
	{
		Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", oldAndroidAvdHome);

		DeleteDir(tempAndroidAvdHome);
	}
}
