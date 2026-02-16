#nullable enable
using System.IO;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// A base class for all tests that will require the Android SDK to be installed
/// and ready to use without having to first download and update it.
/// </summary>
public abstract class AndroidSdkManagerTestsBase : TestsBase
{
	public AndroidSdkManagerTestsBase(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
		: base(outputHelper)
	{
		AndroidSdkHome = fixture.AndroidSdkHome;
		Sdk = fixture.Sdk;
	}

	public DirectoryInfo AndroidSdkHome { get; }

	public AndroidSdkManager Sdk { get; }
}
