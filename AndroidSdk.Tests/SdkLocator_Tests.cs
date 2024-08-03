using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class SdkLocator_Tests : TestsBase
{
	public SdkLocator_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void LocatedPath()
	{
		var l = new SdkLocator();
		var p = l.Locate();

		Assert.NotNull(p);
	}
}
