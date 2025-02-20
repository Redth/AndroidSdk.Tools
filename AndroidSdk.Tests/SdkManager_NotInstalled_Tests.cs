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
}
