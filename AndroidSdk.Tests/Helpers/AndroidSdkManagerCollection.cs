#nullable enable
using Xunit;

namespace AndroidSdk.Tests;

[CollectionDefinition(Name)]
public class AndroidSdkManagerCollection : ICollectionFixture<AndroidSdkManagerFixture>, ICollectionFixture<BootedEmulatorFixture>
{
	public const string Name = "Android SDK Manager";

	// This class has no code, and is never created. Its purpose is simply
	// to be the place to apply [CollectionDefinition] and all the
	// ICollectionFixture<> interfaces.
}
