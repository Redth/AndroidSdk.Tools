using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests
{
	public class Adbd_Tests : TestsBase
	{
		public Adbd_Tests(ITestOutputHelper outputHelper)
			: base(outputHelper)
		{
			AndroidSdkHome = new SdkLocator().Locate().First();
		}

		DirectoryInfo AndroidSdkHome;

		// TODO: this needs to be written for a CI scenario
		[Fact(Skip = SkipOnCI)]
		public async Task GetVersion()
		{
			var adbclient = new AdbdClient(AndroidSdkHome);

			var v = await adbclient.GetHostVersionAsync();

			OutputHelper.WriteLine($"Version: {v}");

			Assert.True(v > 0);
		}

		// TODO: this needs to be written for a CI scenario
		[Fact(Skip = SkipOnCI)]
		public async Task GetShellProps()
		{
			var adbclient = new AdbdClient(AndroidSdkHome);

			var avdName = await adbclient.GetPropAsync("emulator-5554", AdbdClient.ShellProperties.AvdName);
			var arch = await adbclient.GetPropAsync("emulator-5554", AdbdClient.ShellProperties.ProductCpuAbi);

			OutputHelper.WriteLine($"Props: {avdName} {arch}");

			Assert.NotEmpty(avdName);
		}

		// TODO: this needs to be written for a CI scenario
		[Fact(Skip = SkipOnCI)]
		public async Task WatchDevices()
		{
			var a = new AdbdClient(AndroidSdkHome);

			var cts = new CancellationTokenSource();
			cts.CancelAfter(60000);

			await a.WatchDevicesAsync(cts.Token, d =>
			{
				OutputHelper.WriteLine($"{d.Serial} -> {d.Device} -> {d.State}");
				return Task.CompletedTask;
			});
		}
	}
}
