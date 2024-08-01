using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidSdk
{
	internal class ProcessRunner
	{
		readonly List<string> standardOutput;
		readonly List<string> standardError;
		readonly List<string> output;
		readonly Process process;

		public ProcessRunner(FileInfo executable, ProcessArgumentBuilder builder)
			: this (executable, builder, System.Threading.CancellationToken.None)
		{ }

		public ProcessRunner(FileInfo executable, ProcessArgumentBuilder builder, System.Threading.CancellationToken cancelToken, bool redirectStandardInput = false)
		{
			standardOutput = new List<string>();
			standardError = new List<string>();
			output = new List<string>();

			//* Create your Process
			process = new Process();
			process.StartInfo.FileName = executable.FullName;
			process.StartInfo.Arguments = builder.ToString();
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;

			if (redirectStandardInput)
				process.StartInfo.RedirectStandardInput = true;

			process.OutputDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					standardOutput.Add(e.Data);
					output.Add(e.Data);
				}
			};
			process.ErrorDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					standardError.Add(e.Data);
					output.Add(e.Data);
				}
			};
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

		public List<string> StandardOutput => standardOutput;

		public List<string> StandardError => standardError;

		public List<string> Output => output;

		public bool HasStandardOutput => standardOutput.Count > 0;

		public bool HasStandardError => standardError.Count > 0;

		public bool HasOutput => output.Count > 0;

		public ProcessResult WaitForExit()
		{
			process.WaitForExit();

			if (standardError?.Any(l => l?.Contains("error: more than one device/emulator") ?? false) ?? false)
				throw new InvalidOperationException("More than one Device/Emulator detected, you must specify which Serial to target.");

			return new ProcessResult(standardOutput, standardError, output, process.ExitCode);
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

	public class ProcessResult
	{
		public readonly List<string> StandardOutput;
		public readonly List<string> StandardError;
		public readonly List<string> Output;

		public readonly int ExitCode;

		public bool Success
			=> ExitCode == 0;

		public string GetAllOutput()
			=> string.Join(Environment.NewLine, Output);

		public string GetOutput()
			=> string.Join(Environment.NewLine, StandardOutput);

		public string GetError()
			=> string.Join(Environment.NewLine, StandardError);

		internal ProcessResult(List<string> stdOut, List<string> stdErr, List<string> output, int exitCode)
		{
			StandardOutput = stdOut;
			StandardError = stdErr;
			Output = output;
			ExitCode = exitCode;
		}
	}
}
