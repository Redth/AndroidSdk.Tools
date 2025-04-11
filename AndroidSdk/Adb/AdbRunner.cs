using System.IO;
using System.Threading;

namespace AndroidSdk
{
	internal class AdbRunner(SdkTool sdkTool)
	{
		internal void AddSerial(string? serial, ProcessArgumentBuilder builder)
		{
			if (!string.IsNullOrEmpty(serial))
			{
				builder.Append("-s");
				builder.AppendQuoted(serial!);
			}
		}

		FileInfo? adbToolPath;

		internal ProcessResult RunAdb(ProcessArgumentBuilder builder, CancellationToken cancelToken = default)
		{
			var locator = new AdbToolLocator();

			adbToolPath ??= locator.FindTool(sdkTool.AndroidSdkHome);
			if (adbToolPath == null || !File.Exists(adbToolPath.FullName))
				throw new FileNotFoundException($"Could not find {locator.ToolName}", adbToolPath?.FullName);

			var p = new ProcessRunner(adbToolPath, builder, cancelToken);

			var r = p.WaitForExit();

			if (r.ExitCode != 0)
				throw new SdkToolFailedExitException(locator.ToolName, r.ExitCode, r.StandardError, r.StandardOutput);

			return r;
		}
	}
}
