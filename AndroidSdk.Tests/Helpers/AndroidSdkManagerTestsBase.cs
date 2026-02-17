#nullable enable
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// Base class for tests that require a ready Android SDK fixture.
/// It only injects SDK dependencies and does not own SDK lifecycle/setup state.
/// </summary>
[Collection(nameof(AndroidSdkManagerCollection))]
public abstract class AndroidSdkManagerTestsBase(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
	: TestsBase(outputHelper)
{
	public DirectoryInfo AndroidSdkHome { get; } = fixture.AndroidSdkHome;

	public AndroidSdkManager Sdk { get; } = fixture.Sdk;
}
