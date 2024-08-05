#nullable enable
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class SdkManager_NotInstalled_Tests : TestsBase
{
	public SdkManager_NotInstalled_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Theory]
	[InlineData(new[] { "1.0", "2.0" }, new[] { "1.0", "2.0" })]
	[InlineData(new[] { "2.0", "1.0" }, new[] { "1.0", "2.0" })]
	[InlineData(new[] { "2.0", "latest" }, new[] { "2.0", "latest" })]
	[InlineData(new[] { "1.0", "latest" }, new[] { "1.0", "latest" })]
	[InlineData(new[] { "latest", "1.0" }, new[] { "1.0", "latest" })]
	[InlineData(new[] { "latest", "2.0" }, new[] { "2.0", "latest" })]
	[InlineData(new[] { "20.0", "latest" }, new[] { "20.0", "latest" })]
	[InlineData(new[] { "11.0", "latest" }, new[] { "11.0", "latest" })]
	[InlineData(new[] { "latest", "11.0" }, new[] { "11.0", "latest" })]
	[InlineData(new[] { "latest", "20.0" }, new[] { "20.0", "latest" })]
	[InlineData(new[] { "11.0", "7.0" }, new[] { "7.0", "11.0" })]
	[InlineData(new[] { "11.0", "7.0", "latest" }, new[] { "7.0", "11.0", "latest" })]
	public void TestDirectoryVersionSort(string[] input, string[] expected)
	{
		var output = input.ToList();
		output.Sort(SdkManager.CmdLineToolsVersionComparer.Default);

		Assert.Equal(expected, output);
	}
}
