using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class JdkLocator_Tests : TestsBase
{
	public JdkLocator_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void LocatedPath()
	{
		var l = new JdkLocator();
		var p = l.Locate();

		Assert.NotNull(p);
	}
}
