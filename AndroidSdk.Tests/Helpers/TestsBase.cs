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
public abstract class TestsBase
{
	protected const string? SkipOnCI =
#if IS_ON_CI
		"This test cannot run on CI.";
#else
		null;
#endif

	public TestsBase(ITestOutputHelper outputHelper)
	{
		OutputHelper = outputHelper;
	}

	public ITestOutputHelper OutputHelper { get; private set; }

	public static string TestAssemblyDirectory => Utils.TestAssemblyDirectory;

	public static string TestDataDirectory => Utils.TestDataDirectory;

	protected void Retry(Func<bool> test, Action action)
	{
		var tries = 10;
		while (test() && tries > 0)
		{
			try
			{
				action();
			}
			catch
			{
			}

			if (test())
			{
				Thread.Sleep(500);
				tries--;
			}
		}

		if (test())
			Assert.Fail("Timed out trying to perform the action.");
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

	internal void WaitForOutput(ProcessRunner runner, int timeout = 5_000)
	{
		var cts = new CancellationTokenSource(timeout);
		while (!cts.IsCancellationRequested && !runner.HasExited && !runner.HasOutput)
		{
			Thread.Sleep(100);
		}
		Assert.True(runner.HasOutput);
	}

	internal int WaitForOutput(ProcessRunner runner, string output, int outputOffset = 0, int timeout = 5_000)
	{
		var cts = new CancellationTokenSource(timeout);
		while (!cts.IsCancellationRequested && !runner.HasExited && IndexOfNoUnicode(runner.Output, output, outputOffset) == -1)
		{
			Thread.Sleep(100);
		}

		var runnerOutput = runner.Output;
		var index = IndexOfNoUnicode(runnerOutput, output, outputOffset);
		if (index == -1)
		{
			OutputHelper.WriteLine($"Expected output '{output}' not found.");
			WriteOutput(runner);
			WriteOutput(runner, true);

			Assert.Contains(output, runnerOutput);
		}

		return index;
	}

	internal void WriteOutput(ProcessRunner runner, bool cleaned = false)
	{
		OutputHelper.WriteLine(cleaned ? "Cleaned output:" : "Output:");
		foreach (var line in runner.Output)
		{
			OutputHelper.WriteLine(cleaned ? RemoveUnicode(line) : line);
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

	internal static string RemoveUnicode(string input)
	{
		List<char> chars = new();
		for (var i = 0; i < input.Length; i++)
		{
			if (input[i] == 27)
				i += 6;
			else
				chars.Add(input[i]);
		}
		return new(chars.ToArray());
	}

	internal static IEnumerable<string> RemoveUnicode(IEnumerable<string> input)
	{
		foreach (var line in input)
			yield return RemoveUnicode(line);
	}

	private static int IndexOfNoUnicode(IReadOnlyList<string> output, string value, int startIndex = 0)
	{
		for (var i = startIndex; i < output.Count; i++)
		{
			if (RemoveUnicode(output[i]) == value)
				return i;
		}
		return -1;
	}
}
