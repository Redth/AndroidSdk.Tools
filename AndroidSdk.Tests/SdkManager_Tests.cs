using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests
{
	public class SdkManager_Tests : TestsBase
	{
		public SdkManager_Tests(ITestOutputHelper outputHelper)
			: base(outputHelper)
		{ }

		[Fact]
		public void LocatedPath()
		{
			var l = new SdkLocator();
			var p = l.Locate();

			Assert.NotNull(p);
		}

		[Fact]
		public void LocatedJdkPath()
		{
			var l = new JdkLocator();
			var p = l.Locate();

			Assert.NotNull(p);
		}

		[Fact]
		public void DownloadSdk()
		{
			var sdk = GetSdk();

			var isUpToDate = sdk.SdkManager.IsUpToDate();

			Assert.True(isUpToDate);
		}

		[Fact]
		public void List()
		{
			var sdk = GetSdk();

			var list = sdk.SdkManager.List();

			Assert.NotNull(list);

			foreach (var a in list.AvailablePackages)
				OutputHelper.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");

			foreach (var a in list.InstalledPackages)
				OutputHelper.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");
		}

		[Fact]
		public void GetLicenses()
		{
			var sdk = GetSdk();

			var list = sdk.SdkManager.GetLicenses();

			Assert.NotNull(list);
		}

		[Fact]
		public void GetAcceptedLicenseIds()
		{
			var sdk = GetSdk();

			var list = sdk.SdkManager.GetAcceptedLicenseIds();

			Assert.NotNull(list);
		}

		[Fact]
		public void Install()
		{
			var sdk = GetSdk();

			var ok = sdk.SdkManager.Install("extras;google;auto");

			Assert.True(ok);

			var list = sdk.SdkManager.List();

			Assert.NotNull(list.InstalledPackages.FirstOrDefault(ip => ip.Path == "extras;google;auto"));
		}

		[Fact]
		public void AcceptLicense()
		{
			var sdk = GetSdk();
			sdk.SdkManager.AcceptLicenses();

			var list = sdk.SdkManager.List();

			Assert.NotNull(list.InstalledPackages);
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
}
