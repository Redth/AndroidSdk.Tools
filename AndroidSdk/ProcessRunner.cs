using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSdk
{
	internal class ProcessRunner : IDisposable
	{
		readonly List<string> standardOutput;
		readonly List<string> standardError;
		readonly List<string> output;
		readonly Process process;
		readonly object outputLock = new();
		readonly StreamWriter durableOutput;
		readonly StreamWriter durableError;
		Exception logError;
		bool logsClosed;
		readonly TaskCompletionSource<bool> standardOutputClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);
		readonly TaskCompletionSource<bool> standardErrorClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
			: this(executable, builder, cancelToken, redirectStandardInput, null, null)
		{
		}

		internal ProcessRunner(FileInfo executable, ProcessArgumentBuilder builder, CancellationToken cancelToken, bool redirectStandardInput, string stdoutPath, string stderrPath)
		{
			if (stdoutPath != null)
			{
				durableOutput = new StreamWriter(new FileStream(stdoutPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
				try
				{
					durableError = new StreamWriter(new FileStream(stderrPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
				}
				catch
				{
					durableOutput.Dispose();
					throw;
				}
			}
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
					Capture(e.Data, standardOutput, durableOutput);

					if ((logTypes & AndroidToolProcessRunnerLogTypes.Stdout) != 0)
						Log(e.Data + Environment.NewLine, logPath, AndroidToolProcessRunnerLogTypes.Stdout);
				}
				else
				{
					standardOutputClosed.TrySetResult(true);
				}
			};
			process.ErrorDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					Capture(e.Data, standardError, durableError);

					if ((logTypes & AndroidToolProcessRunnerLogTypes.Stderr) != 0)
						Log(e.Data + Environment.NewLine, logPath, AndroidToolProcessRunnerLogTypes.Stderr);
				}
				else
				{
					standardErrorClosed.TrySetResult(true);
				}
			};
			try
			{
				process.Start();
			}
			catch
			{
				durableOutput?.Dispose();
				durableError?.Dispose();
				process.Dispose();
				throw;
			}
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

		internal Process NativeProcess => process;

		public void Dispose() => process.Dispose();

		void Capture(string line, List<string> target, StreamWriter writer)
		{
			lock (outputLock)
			{
				target.Add(line);
				output.Add(line);
				if (durableOutput != null)
				{
					if (target.Count > 200)
						target.RemoveAt(0);
					if (output.Count > 200)
						output.RemoveAt(0);
				}
				if (logError == null && writer != null)
				{
					try { writer.WriteLine(line); }
					catch (Exception ex) { logError = ex; }
				}
			}
		}

		internal void ThrowIfLogFailed()
		{
			lock (outputLock)
				if (logError != null)
					throw new IOException("Writing emulator stdout/stderr failed.", logError);
		}

		internal void KillAndWait()
		{
			if (!process.HasExited)
			{
				try
				{
#if NETSTANDARD2_0
					process.Kill();
#else
					process.Kill(entireProcessTree: true);
#endif
				}
				catch (InvalidOperationException) when (process.HasExited) { }
			}
			if (!process.WaitForExit(10000))
				throw new TimeoutException("The exact child process did not terminate within 10 seconds.");
		}

		public ProcessResult WaitForExit(CancellationToken cancellationToken)
		{
			while (!process.WaitForExit(100))
			{
				if (cancellationToken.IsCancellationRequested)
				{
					KillAndWait();
					cancellationToken.ThrowIfCancellationRequested();
				}
			}
			cancellationToken.ThrowIfCancellationRequested();
			return CompleteOutput(cancellationToken);
		}

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

		public List<string> StandardOutput { get { lock (outputLock) return new List<string>(standardOutput); } }

		public List<string> StandardError { get { lock (outputLock) return new List<string>(standardError); } }

		public List<string> Output { get { lock (outputLock) return new List<string>(output); } }

		public bool HasStandardOutput => standardOutput.Count > 0;

		public bool HasStandardError => standardError.Count > 0;

		public bool HasOutput => output.Count > 0;

		public ProcessResult WaitForExit()
		{
			while (!process.WaitForExit(1000)) { }
			return CompleteOutput(CancellationToken.None);
		}

		ProcessResult CompleteOutput(CancellationToken cancellationToken)
		{
			if (!Task.WaitAll(new[] { standardOutputClosed.Task, standardErrorClosed.Task }, 5000, cancellationToken))
				throw new TimeoutException("Child process stdout/stderr did not close within 5 seconds.");

			lock (outputLock)
			{
				if (!logsClosed)
				{
					logsClosed = true;
					try
					{
						durableOutput?.Flush();
						durableError?.Flush();
						(durableOutput?.BaseStream as FileStream)?.Flush(true);
						(durableError?.BaseStream as FileStream)?.Flush(true);
					}
					finally
					{
						try { durableOutput?.Dispose(); }
						finally { durableError?.Dispose(); }
					}
				}
			}
			ThrowIfLogFailed();

			if (standardError?.Any(l => l?.Contains("error: more than one device/emulator") ?? false) ?? false)
				throw new InvalidOperationException("More than one Device/Emulator detected, you must specify which Serial to target.");

			return new ProcessResult(StandardOutput, StandardError, Output, process.ExitCode);
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
