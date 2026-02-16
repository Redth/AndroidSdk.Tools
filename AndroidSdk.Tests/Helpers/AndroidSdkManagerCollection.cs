#nullable enable
using Xunit;

namespace AndroidSdk.Tests;

/// <summary>
/// SDK-scoped test collection.
/// Provides only the shared Android SDK fixture and does not own AVD/emulator state.
/// </summary>
[CollectionDefinition(Name)]
public class AndroidSdkManagerCollection : ICollectionFixture<AndroidSdkManagerFixture>
{
	public const string Name = "Android SDK Manager";

	// This class has no code, and is never created. Its purpose is simply
	// to be the place to apply [CollectionDefinition] and all the
	// ICollectionFixture<> interfaces.
}
