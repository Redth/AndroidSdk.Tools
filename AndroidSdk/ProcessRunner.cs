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

		/// <summary>
		/// Parses the log types from the environment variable string.
		/// </summary>
		/// <param name="logTypesStr">The log types string from the environment variable.</param>
		/// <returns>The parsed log types as flags.</returns>
		private static AndroidToolProcessRunnerLogTypes ParseLogTypes(string? logTypesStr)
		{
			if (string.IsNullOrWhiteSpace(logTypesStr))
				return AndroidToolProcessRunnerLogTypes.Stdout | AndroidToolProcessRunnerLogTypes.Stderr | AndroidToolProcessRunnerLogTypes.Stdin;

			var result = (AndroidToolProcessRunnerLogTypes)0;
			foreach (var type in logTypesStr.Split([ '|' ], StringSplitOptions.RemoveEmptyEntries))
			{
				if (type.Equals("stdout", StringComparison.OrdinalIgnoreCase))
					result |= AndroidToolProcessRunnerLogTypes.Stdout;
				else if (type.Equals("stderr", StringComparison.OrdinalIgnoreCase))
					result |= AndroidToolProcessRunnerLogTypes.Stderr;
				else if (type.Equals("stdin", StringComparison.OrdinalIgnoreCase))
					result |= AndroidToolProcessRunnerLogTypes.Stdin;
			}
			return result;
		}

		/// <summary>
		/// Logs a message to the specified log type.
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="type">The log type.</param>
		void Log(string message, string logPath, AndroidToolProcessRunnerLogTypes logType)
		{
			if (string.IsNullOrWhiteSpace(logPath))
				return;

			var m = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logType.ToString().ToLowerInvariant()}] {message}";

			try {
				File.AppendAllText(logPath, m);
			} catch { }
		}

		[Flags]
		enum AndroidToolProcessRunnerLogTypes
		{
			/// <summary>Standard output logging</summary>
			Stdout = 1,
			/// <summary>Standard error logging</summary>
			Stderr = 2,
			/// <summary>Standard input logging</summary>
			Stdin = 4
		}

		string? logPath = null;
		AndroidToolProcessRunnerLogTypes logTypes = AndroidToolProcessRunnerLogTypes.Stdout | AndroidToolProcessRunnerLogTypes.Stderr | AndroidToolProcessRunnerLogTypes.Stdin;

		public ProcessRunner(FileInfo executable, ProcessArgumentBuilder builder, CancellationToken cancelToken, bool redirectStandardInput = false)
		{
			logPath = Environment.GetEnvironmentVariable("ANDROID_TOOL_PROCESS_RUNNER_LOG_PATH");
			logTypes = ParseLogTypes(Environment.GetEnvironmentVariable("ANDROID_TOOL_PROCESS_RUNNER_LOG_TYPES"));

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

					if ((logTypes & AndroidToolProcessRunnerLogTypes.Stdout) != 0)
						Log(e.Data + Environment.NewLine, logPath, AndroidToolProcessRunnerLogTypes.Stdout);
				}
			};
			process.ErrorDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					standardError.Add(e.Data);
					output.Add(e.Data);

					if ((logTypes & AndroidToolProcessRunnerLogTypes.Stderr) != 0)
						Log(e.Data + Environment.NewLine, logPath, AndroidToolProcessRunnerLogTypes.Stderr);
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

			if ((logTypes & AndroidToolProcessRunnerLogTypes.Stdin) != 0)
				Log(input, logPath, AndroidToolProcessRunnerLogTypes.Stdin);
		}

		public void StandardInputWriteLine(string input)
		{
			if (!process.StartInfo.RedirectStandardInput)
				throw new InvalidOperationException();

			process.StandardInput.WriteLine(input);

			if ((logTypes & AndroidToolProcessRunnerLogTypes.Stdin) != 0)
				Log(input + Environment.NewLine, logPath, AndroidToolProcessRunnerLogTypes.Stdin);
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
