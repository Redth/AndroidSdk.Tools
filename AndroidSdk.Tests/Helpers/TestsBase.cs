#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// A base class for all tests in this assembly.
/// </summary>
public abstract class TestsBase : IDisposable
{
	protected const bool IsCI =
#if IS_ON_CI
		true;
#else
		false;
#endif

	protected const string? SkipOnCI =
		IsCI ? "This test cannot run on CI." : null;

    protected const string StaticAppPackageName = "com.companyname.mauiapp12345";

    protected static readonly string StaticAppApkPath = Path.GetFullPath(Path.Combine(TestDataDirectory, "com.companyname.mauiapp12345-Signed.apk"));

	private readonly string logFile;
	private readonly IDisposable processRunnerLogPathScope;

	public TestsBase(ITestOutputHelper outputHelper)
	{
		OutputHelper = outputHelper;

		logFile = Path.GetTempFileName();
		processRunnerLogPathScope = new EnvironmentVariablesScope(("ANDROID_TOOL_PROCESS_RUNNER_LOG_PATH", logFile));
	}

	public virtual void Dispose()
	{
		try
		{
			if (File.Exists(logFile))
			{
				OutputHelper.WriteLine($"Process runner log ({logFile}):");
				foreach (var line in File.ReadLines(logFile))
					OutputHelper.WriteLine(line);
			}
		}
		finally
		{
			try
			{
				if (File.Exists(logFile))
					File.Delete(logFile);
			}
			catch (IOException ex)
			{
				OutputHelper.WriteLine($"Failed to delete log file {logFile}: {ex.Message}");
			}
			catch (UnauthorizedAccessException ex)
			{
				OutputHelper.WriteLine($"Failed to delete log file {logFile}: {ex.Message}");
			}

			processRunnerLogPathScope.Dispose();
		}
	}

	public ITestOutputHelper OutputHelper { get; private set; }

	public static string TestAssemblyDirectory => Utils.TestAssemblyDirectory;

	public static string TestDataDirectory => Utils.TestDataDirectory;

	protected void Retry(Func<bool> test, Action action)
	{
		Exception? lastException = null;
		var tries = 10;
		while (test() && tries > 0)
		{
			try
			{
				action();
				lastException = null;
			}
			catch (IOException ex)
			{
				lastException = ex;
			}
			catch (UnauthorizedAccessException ex)
			{
				lastException = ex;
			}

			if (test())
			{
				Thread.Sleep(500);
				tries--;
			}
		}

		if (test())
		{
			if (lastException is not null)
				OutputHelper.WriteLine($"Last retry error: {lastException}");
			Assert.Fail("Timed out trying to perform the action.");
		}
	}

	protected void DeleteDir(string dir) =>
		Retry(() => Directory.Exists(dir), () => Directory.Delete(dir, true));

	protected void CreateDir(string dir) =>
		Retry(() => !Directory.Exists(dir), () => Directory.CreateDirectory(dir));

	protected void RecreateDir(string dir)
	{
		DeleteDir(dir);
		CreateDir(dir);
	}

	protected static FileInfo? GetFileOnPath(string fileName)
	{
		var paths = Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrWhiteSpace(paths))
			return null;

		foreach (var path in paths.Split(Path.PathSeparator))
		{
			// try exact path
			var fullPath = Path.Combine(path, fileName);
			if (File.Exists(fullPath))
				return new FileInfo(fullPath);

			if (OperatingSystem.IsWindows())
			{
				// try with .exe extension
				fullPath = Path.Combine(path, fileName + ".exe");
				if (File.Exists(fullPath))
					return new FileInfo(fullPath);
			}
		}

		return null;
	}

	internal void WaitForOutput(ProcessRunner runner, int timeout = 10_000)
	{
		var cts = new CancellationTokenSource(timeout);
		while (!cts.IsCancellationRequested && !runner.HasExited && !runner.HasOutput)
		{
			Thread.Sleep(250);
		}
		Assert.True(runner.HasOutput);
	}

	internal int WaitForOutput(ProcessRunner runner, string output, int outputOffset = 0, int timeout = 10_000, Func<string, string>? selector = null)
	{
		selector ??= s => s;
		Func<IEnumerable<string>> filtered = () => runner.Output.Skip(outputOffset).Select(selector);

		var cts = new CancellationTokenSource(timeout);
		while (!cts.IsCancellationRequested && !runner.HasExited && !filtered().Contains(output))
		{
			Thread.Sleep(250);
		}

		var index = filtered().ToList().IndexOf(output);
		index += outputOffset;
		if (index == -1)
		{
			OutputHelper.WriteLine($"Expected output '{output}' not found.");
			WriteOutput(runner);
			Assert.Contains(output, filtered());
		}

		return index;
	}

	internal void WriteOutput(ProcessRunner runner)
	{
		OutputHelper.WriteLine("Output:");
		foreach (var line in runner.Output)
		{
			OutputHelper.WriteLine(line);
		}
	}

	internal void WriteOutput(ProcessResult result)
	{
		OutputHelper.WriteLine("Output:");
		foreach (var line in result.Output)
		{
			OutputHelper.WriteLine(line);
		}
	}
}
