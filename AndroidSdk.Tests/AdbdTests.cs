using System;
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
		{ }

		[Fact]
		public async Task GetVersion()
		{
			var adbclient = new AdbdClient();

			var v = await adbclient.GetHostVersionAsync();

			OutputHelper.WriteLine($"Version: {v}");

			Assert.True(v > 0);
		}

		[Fact]
		public async Task GetShellProps()
		{
			var adbclient = new AdbdClient();

			var avdName = await adbclient.GetPropAsync("emulator-5554", AdbdClient.ShellProperties.AvdName);
			var arch = await adbclient.GetPropAsync("emulator-5554", AdbdClient.ShellProperties.ProductCpuAbi);

			OutputHelper.WriteLine($"Props: {avdName} {arch}");

			Assert.NotEmpty(avdName);
		}

		[Fact]
		public async Task WatchDevices()
		{
			var a = new AdbdClient();

			var cts = new CancellationTokenSource();
			cts.CancelAfter(60000);

			await a.WatchDevicesAsync(cts.Token, async d =>
			{
				OutputHelper.WriteLine($"{d.serial} -> {d.device} -> {d.state}");


			}).ConfigureAwait(false);

			
		}
	}
}
