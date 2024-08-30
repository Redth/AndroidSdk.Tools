using System;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class MonoDroidSdkLocator_Tests : TestsBase
{
	public MonoDroidSdkLocator_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void LocatePaths()
	{
		var location
			= OperatingSystem.IsWindows()
				? MonoDroidSdkLocator.ReadRegistry()
				: MonoDroidSdkLocator.ReadConfigFile();

		Assert.NotNull(location.JavaJdkPath);
		Assert.NotNull(location.AndroidSdkPath);
	}
}
