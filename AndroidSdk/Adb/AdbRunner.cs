using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AndroidSdk
{
	internal class AdbRunner
	{
		public AdbRunner(SdkTool sdkTool)
		{
			this.sdkTool = sdkTool;
		}

		SdkTool sdkTool;

		internal void AddSerial(string serial, ProcessArgumentBuilder builder)
		{
			if (!string.IsNullOrEmpty(serial))
			{
				builder.Append("-s");
				builder.AppendQuoted(serial);
			}
		}

		internal ProcessResult RunAdb(DirectoryInfo androidSdkHome, ProcessArgumentBuilder builder)
			=> RunAdb(androidSdkHome, builder, System.Threading.CancellationToken.None);

		internal ProcessResult RunAdb(DirectoryInfo androidSdkHome, ProcessArgumentBuilder builder, System.Threading.CancellationToken cancelToken)
		{
			var adbToolPath = sdkTool.FindToolPath(androidSdkHome);
			if (adbToolPath == null || !File.Exists(adbToolPath.FullName))
				throw new FileNotFoundException("Could not find adb", adbToolPath?.FullName);

			var p = new ProcessRunner(adbToolPath, builder, cancelToken);

			var r = p.WaitForExit();

			if (r.ExitCode != 0)
			{
				throw new SdkToolFailedExitException("adb", r.ExitCode, r.StandardError, r.StandardOutput);
			}

			return r;
		}
	}
}
