#nullable enable
using Xunit;

namespace AndroidSdk.Tests;

/// <summary>
/// Collection fixture for Android SDK manager tests.
/// It provides a shared AndroidSdkManagerFixture instance for all tests in the collection and ensures proper cleanup of any temporary SDKs created during testing.
/// </summary>
[CollectionDefinition(nameof(AndroidSdkManagerCollection))]
public class AndroidSdkManagerCollection : ICollectionFixture<AndroidSdkManagerFixture>
{
	// This class has no code, and is never created. Its purpose is simply
	// to be the place to apply [CollectionDefinition] and all the
	// ICollectionFixture<> interfaces.
}
