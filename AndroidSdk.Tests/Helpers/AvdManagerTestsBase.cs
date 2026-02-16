using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// AVD manager test base class.
/// Keeps emulator-related tests on the shared Android SDK fixture while moving AVD-home ownership to explicit scopes.
/// </summary>
public abstract class AvdManagerTestsBase(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
	: AndroidSdkManagerTestsBase(outputHelper, fixture)
{
}
