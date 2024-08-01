using System;
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
		runner.WaitForOutput("Hello, World!");

		runner.StandardInputWriteLine("exit");

		var result = runner.WaitForExit();

		var expectedOutput = new[]
		{
			"PS>Write-Host 'Hello, World!'",
			"Hello, World!",
			"PS>exit"
		};

		Assert.Equal(0, result.ExitCode);
		Assert.Equal(expectedOutput, result.Output);
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
		runner.WaitForOutput("\u001b[31;1mWrite-Error: \u001b[31;1mBad Things!\u001b[0m");

		runner.StandardInputWriteLine("exit 3");

		var result = runner.WaitForExit();

		var expectedOutput = new[]
		{
			"\u001b[31;1mWrite-Error: \u001b[31;1mBad Things!\u001b[0m",
			"PS>exit 3"
		};

		var expectedError = new[]
		{
			"\u001b[31;1mWrite-Error: \u001b[31;1mBad Things!\u001b[0m",
		};

		Assert.Equal(3, result.ExitCode);
		Assert.Equal(expectedOutput, result.Output.Skip(1));
		Assert.Equal(expectedError, result.StandardError);
		Assert.Equal(expectedOutput, result.Output.Skip(1));
	}

	[Fact]
	public void RunJava()
	{
		var jdkLocator = new JdkLocator();
		var jdk = jdkLocator.LocateJdk().FirstOrDefault();
		Assert.NotNull(jdk);

		var jarFile = Path.GetFullPath(Path.Combine(TestDataDirectory, "EchoApp.jar"));

		var javaArgs = new JavaProcessArgumentBuilder("com.androidsdk.EchoApp");

		javaArgs.AppendQuoted("Hello, World!");

		//// Set the classpath to all the .jar files we found in the lib folder
		//javaArgs.AppendClassPath(Directory.GetFiles(libPath, "*.jar").Select(f => new FileInfo(f).Name));

		//// This needs to be set to the working dir / classpath dir as the library looks for this system property at runtime
		//javaArgs.AppendJavaToolOption($"-Dcom.android.sdklib.toolsdir=\"{toolPath}\"");

		//// lib folder is our working dir
		//javaArgs.SetWorkingDirectory(libPath);

		var runner = new JavaProcessRunner(jdk, javaArgs);

		var result = runner.WaitForExit();

		Assert.Equal("Arguments: Hello, World!", result.GetAllOutput());
	}
}
