using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class ProcessRunner_Tests : TestsBase
{
	private FileInfo pwsh => GetFileOnPath("pwsh");

	public ProcessRunner_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void RunSimpleCommand()
	{
		// pwsh -c "echo 'Hello, World!'"
		var args = new ProcessArgumentBuilder();
		args.Append("-c");
		args.AppendQuoted("echo 'Hello, World!'");

		var runner = new ProcessRunner(pwsh, args);
		var result = runner.WaitForExit();

		Assert.Equal(0, result.ExitCode);
		Assert.Equal("Hello, World!", result.GetOutput());
		Assert.Equal("", result.GetError());
		Assert.Equal("Hello, World!", result.GetAllOutput());
	}

	[Fact]
	public void RunSimpleCommandWithError()
	{
		// pwsh -c "echo 'Hello, World!'"
		var args = new ProcessArgumentBuilder();
		args.Append("-c");
		args.AppendQuoted("Write-Error 'Bad Things!'; exit 3;");

		var runner = new ProcessRunner(pwsh, args);
		var result = runner.WaitForExit();

		Assert.Equal(3, result.ExitCode);
		Assert.Equal("", result.GetOutput());
		Assert.Contains("Bad Things!", result.GetError());
		Assert.Contains("Bad Things!", result.GetAllOutput());
	}

	[Fact]
	public void RunInput()
	{
		var args = new ProcessArgumentBuilder();
		args.Append("-NoLogo");
		args.Append("-NoExit");
		args.Append("-Command");
		args.AppendQuoted("function Prompt { '' }");

		var runner = new ProcessRunner(pwsh, args, default, true);

		Assert.False(runner.HasExited);
		runner.StandardInputWriteLine("Write-Host 'Hello, World!'");

		Assert.False(runner.HasExited);
		WaitForOutput(runner, "Hello, World!");

		runner.StandardInputWriteLine("exit");

		var result = runner.WaitForExit();

		var expectedOutput = new[]
		{
			"PS>Write-Host 'Hello, World!'",
			"Hello, World!",
			"PS>exit"
		};

		Assert.Equal(0, result.ExitCode);
		Assert.Equal(expectedOutput, result.StandardOutput);
		Assert.Empty(result.StandardError);
		Assert.Equal(expectedOutput, result.Output);
	}

	[Fact]
	public void RunInputWithError()
	{
		var args = new ProcessArgumentBuilder();
		args.Append("-NoLogo");
		args.Append("-NoExit");
		args.Append("-Command");
		args.AppendQuoted("function Prompt { '' }");

		var runner = new ProcessRunner(pwsh, args, default, true);

		Assert.False(runner.HasExited);
		runner.StandardInputWriteLine("Write-Error 'Bad Things!'");

		Assert.False(runner.HasExited);
		WaitForOutput(runner, "Write-Error: Bad Things!", selector: RemoveUnicode);

		runner.StandardInputWriteLine("exit 3");

		var result = runner.WaitForExit();

		WriteOutput(result);

		var expectedStdOutput = new[]
		{
			"PS>Write-Error 'Bad Things!'",
			"PS>exit 3"
		};

		var expectedError = new[]
		{
			"Write-Error: Bad Things!",
		};

		var expectedOutput = new[]
		{
			"PS>Write-Error 'Bad Things!'",
			"Write-Error: Bad Things!",
			"PS>exit 3"
		};

		Assert.Equal(3, result.ExitCode);
		Assert.Equal(expectedStdOutput, result.StandardOutput.Select(RemoveUnicode));
		Assert.Equal(expectedError, result.StandardError.Select(RemoveUnicode));
		Assert.Equal(expectedOutput, result.Output.Select(RemoveUnicode));

		static string RemoveUnicode(string input)
		{
			List<char> chars = new();
			for (var i = 0; i < input.Length; i++)
			{
				if (input[i] == 27)
				{
					if (OperatingSystem.IsWindows())
						i += 6;
					else
						i += 4;
				}
				else
				{
					chars.Add(input[i]);
				}
			}
			return new(chars.ToArray());
		}
	}
}
