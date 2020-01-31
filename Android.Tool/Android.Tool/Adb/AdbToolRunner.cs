using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Android.Tool.Adb
{
	public class AdbToolRunner
	{
		internal IEnumerable<FileInfo> FindAdb(AdbToolSettings settings = null)
		{
			var results = new List<FileInfo>();

			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			var ext = isWindows ? ".exe" : "";
			var androidHome = settings.AndroidSdkRoot.FullName;

			if (!Directory.Exists(androidHome))
				androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
			
			if (!string.IsNullOrEmpty(androidHome) && Directory.Exists(androidHome))
			{
				var exe = Path.Combine(androidHome, "platform-tools", "adb" + ext);
				if (File.Exists(exe))
				results.Add(new FileInfo(exe));
			}

			return results;
		}

		internal void AddSerial(string serial, ProcessArgumentBuilder builder)
		{
			if (!string.IsNullOrEmpty(serial))
			{
				builder.Append("-s");
				builder.AppendQuoted(serial);
			}
		}

		internal bool RunAdb(AdbToolSettings settings, ProcessArgumentBuilder builder)
		{
			var output = new List<string>();
			return RunAdb(settings, builder, out output);
		}

		internal bool RunAdb(AdbToolSettings settings, ProcessArgumentBuilder builder, out List<string> output)
		{
			return RunAdb(settings, builder, System.Threading.CancellationToken.None, out output);
		}

		internal bool RunAdb(AdbToolSettings settings, ProcessArgumentBuilder builder, System.Threading.CancellationToken cancelToken, out List<string> output)
		{
			var adbToolPath = FindAdb(settings)?.FirstOrDefault();
			if (adbToolPath == null || !File.Exists(adbToolPath.FullName))
				throw new FileNotFoundException("Could not find adb", adbToolPath?.FullName);

			var lines = new List<string>();
			var err = new List<string>();

			//* Create your Process
			Process process = new Process();
			process.StartInfo.FileName = adbToolPath.FullName;
			process.StartInfo.Arguments = builder.ToString();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			process.OutputDataReceived += (s, e) => lines.Add(e.Data);
			process.ErrorDataReceived += (s, e) => err.Add(e.Data);
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			if (cancelToken != System.Threading.CancellationToken.None)
			{
				cancelToken.Register(() => {
					try { process.Kill(); }
					catch { }
				});
			}
			process.WaitForExit();

			output = lines;

			var error = err?.FirstOrDefault(o => o.StartsWith("error:", StringComparison.OrdinalIgnoreCase));

			if (!string.IsNullOrEmpty(error))
				throw new Exception(error);

			return process.ExitCode == 0;
		}
	}
}
