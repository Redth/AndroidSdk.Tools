using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests
{
	public class AvdManager_Tests : TestsBase
	{
		public AvdManager_Tests(ITestOutputHelper outputHelper)
			: base(outputHelper)
		{ }

		[Fact]
		public void LocatedPath()
		{
			var l = new AvdLocator();
			var p = l.Locate();

			Assert.NotNull(p);
		}

		[Fact]
		public void ListAvds()
		{
			var sdk = GetSdk();
			var avds = sdk.AvdManager.ListAvds();

			Assert.NotEmpty(avds);
		}
	}
}
