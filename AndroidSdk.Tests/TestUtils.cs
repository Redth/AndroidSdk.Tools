using System.Linq;
using System.Threading;
using Xunit;

namespace AndroidSdk.Tests;

static class TestUtils
{
	internal static void WaitForOutput(this ProcessRunner runner, int timeout = 1_000)
	{
		var cts = new CancellationTokenSource(timeout);
		while (!cts.IsCancellationRequested && !runner.HasExited && !runner.HasOutput)
		{
			Thread.Sleep(100);
		}
		Assert.True(runner.HasOutput);
	}

	internal static int WaitForOutput(this ProcessRunner runner, string output, int outputOffset = 0, int timeout = 1_000)
	{
		var cts = new CancellationTokenSource(timeout);
		while (!cts.IsCancellationRequested && !runner.HasExited && runner.Output.IndexOf(output, outputOffset) == -1)
		{
			Thread.Sleep(100);
		}
		Assert.Contains(output, runner.Output.Skip(outputOffset));
		return runner.Output.IndexOf(output, outputOffset);
	}
}
