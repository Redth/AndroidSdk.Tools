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
				Console.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");

			foreach (var a in list.InstalledPackages)
				Console.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");
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
	}
}
