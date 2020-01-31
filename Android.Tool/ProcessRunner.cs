using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Android.Tool
{
	internal class ProcessRunner
	{
		readonly List<string> standardOutput;
		readonly List<string> standardError;
		readonly Process process;

		public ProcessRunner(FileInfo executable, ProcessArgumentBuilder builder)
			: this (executable, builder, System.Threading.CancellationToken.None)
		{ }

		public ProcessRunner(FileInfo executable, ProcessArgumentBuilder builder, System.Threading.CancellationToken cancelToken, bool redirectStandardInput = false)
		{
			standardOutput = new List<string>();
			standardError = new List<string>();

			//* Create your Process
			process = new Process();
			process.StartInfo.FileName = executable.FullName;
			process.StartInfo.Arguments = builder.ToString();
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			if (redirectStandardInput)
				process.StartInfo.RedirectStandardInput = true;

			process.OutputDataReceived += (s, e) => standardOutput.Add(e.Data);
			process.ErrorDataReceived += (s, e) => standardError.Add(e.Data);
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
		}

		public int ExitCode
			=> process.HasExited ? process.ExitCode : -1;

		public bool HasExited
			=> process?.HasExited ?? false;

		public void Kill()
			=> process?.Kill();

		public void StandardInputWrite(string input)
		{
			if (!process.StartInfo.RedirectStandardInput)
				throw new InvalidOperationException();

			process.StandardInput.Write(input);
		}

		public void StandardInputWriteLine(string input)
		{
			if (!process.StartInfo.RedirectStandardInput)
				throw new InvalidOperationException();

			process.StandardInput.WriteLine(input);
		}

		public ProcessResult WaitForExit()
		{
			process.WaitForExit();

			var error = standardError?.FirstOrDefault(o => o.StartsWith("error:", StringComparison.OrdinalIgnoreCase));

			if (!string.IsNullOrEmpty(error))
				throw new Exception(error);

			return new ProcessResult(standardOutput, standardError, process.ExitCode);
		}

		public Task<ProcessResult> WaitForExitAsync()
		{
			var tcs = new TaskCompletionSource<ProcessResult>();

			Task.Run(() =>
			{
				var r = WaitForExit();
				tcs.TrySetResult(r);
			});

			return tcs.Task;
		}
	}

	internal class ProcessResult
	{
		public readonly List<string> StandardOutput;
		public readonly List<string> StandardError;

		public readonly int ExitCode;

		public bool Success
			=> ExitCode == 0;

		internal ProcessResult(List<string> stdOut, List<string> stdErr, int exitCode)
		{
			StandardOutput = stdOut;
			StandardError = stdErr;
			ExitCode = exitCode;
		}
	}
}
