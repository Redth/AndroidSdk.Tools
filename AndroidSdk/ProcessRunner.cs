using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
			: this(executable, builder, CancellationToken.None)
		{
		}

		public ProcessRunner(FileInfo executable, ProcessArgumentBuilder builder, CancellationToken cancelToken, bool redirectStandardInput = false)
		{
			standardOutput = new List<string>();
			standardError = new List<string>();
			output = new List<string>();

			process = new Process();
			foreach (var envvar in builder.EnvVars)
			{
				process.StartInfo.Environment[envvar.Key] = envvar.Value;
			}
			process.StartInfo.FileName = executable.FullName;
			process.StartInfo.Arguments = builder.ToString();
			if (!string.IsNullOrWhiteSpace(builder.WorkingDirectory))
				process.StartInfo.WorkingDirectory = builder.WorkingDirectory;
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

			if (cancelToken != CancellationToken.None)
			{
				cancelToken.Register(() =>
				{
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

		public void StandardInputFlush()
		{
			if (!process.StartInfo.RedirectStandardInput)
				throw new InvalidOperationException();

			process.StandardInput.Flush();
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

		internal void LoopUntilExit(Action<ProcessRunner> action)
		{
			while (!HasExited)
			{
				Thread.Sleep(250);

				try
				{
					action(this);
				}
				catch { }
			}
		}

		internal void WriteContinuouslyUntilExit(string input)
		{
			LoopUntilExit(proc =>
			{
				proc.StandardInputWriteLine(input);
				proc.StandardInputFlush();
			});
		}
	}
}
