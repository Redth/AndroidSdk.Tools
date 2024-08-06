#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace AndroidSdk;

public class SdkToolFailedExitException : Exception
{
	public SdkToolFailedExitException(string name, ProcessResult result)
		: this(name, result.ExitCode, result.StandardError, result.StandardOutput, result.Output)
	{
	}

	public SdkToolFailedExitException(string name, int exitCode, IEnumerable<string> stdErr, IEnumerable<string> stdOut)
		: this(name, exitCode, stdErr, stdOut, null)
	{
	}

	public SdkToolFailedExitException(string name, int exitCode, IEnumerable<string> stdErr, IEnumerable<string> stdOut, IEnumerable<string> output)
		: base($"{name} exited with an error status")
	{
		ExitCode = exitCode;
		StdErr = stdErr?.ToArray() ?? new string[0];
		StdOut = stdOut?.ToArray() ?? new string[0];
		AllOut = output?.ToArray() ?? new string[0];
	}

	public readonly int ExitCode;

	public readonly string[] StdOut;

	public readonly string[] StdErr;

	public readonly string[] AllOut;

	internal static void ThrowIfErrorExitCode(string name, ProcessResult result)
	{
		if (result.ExitCode != 0)
			throw new SdkToolFailedExitException(name, result);
	}
}
