using System;
using System.Collections.Generic;

namespace AndroidSdk;

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
