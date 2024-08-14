using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class JavaProcessRunner_Tests : TestsBase
{
	public JavaProcessRunner_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void RunSimpleCommand()
	{
		var jdkLocator = new JdkLocator();
		var jdk = jdkLocator.LocateJdk().FirstOrDefault();
		Assert.NotNull(jdk);

		var args = new ProcessArgumentBuilder();
		args.AppendQuoted("Hello, World!");

		var javaArgs = new JavaProcessArgumentBuilder("com.androidsdk.EchoApp", args);
		javaArgs.AppendClassPath(Directory.GetFiles(TestDataDirectory, "*.jar").Select(f => new FileInfo(f).Name));
		javaArgs.SetWorkingDirectory(TestDataDirectory);

		var runner = new JavaProcessRunner(jdk, javaArgs);

		var result = runner.WaitForExit();

		Assert.Equal("Arguments:" + Environment.NewLine + "Hello, World!", result.GetAllOutput());
	}

	[Fact]
	public void RunInput()
	{
		var jdkLocator = new JdkLocator();
		var jdk = jdkLocator.LocateJdk().FirstOrDefault();
		Assert.NotNull(jdk);

		var args = new ProcessArgumentBuilder();

		var javaArgs = new JavaProcessArgumentBuilder("com.androidsdk.EchoApp", args);
		javaArgs.AppendClassPath(Directory.GetFiles(TestDataDirectory, "*.jar").Select(f => new FileInfo(f).Name));
		javaArgs.SetWorkingDirectory(TestDataDirectory);

		var runner = new JavaProcessRunner(jdk, javaArgs, default, true);

		Assert.False(runner.HasExited);
		runner.StandardInputWriteLine("Hello, World!");

		Assert.False(runner.HasExited);
		WaitForOutput(runner, "You entered: Hello, World!");

		runner.StandardInputWriteLine("exit");

		var result = runner.WaitForExit();

		var expectedOutput = new[]
		{
			"Enter text to echo. Type 'exit' to quit.",
			"You entered: Hello, World!",
			"Exiting..."
		};

		Assert.Equal(0, result.ExitCode);
		Assert.Equal(expectedOutput, result.StandardOutput);
		Assert.Empty(result.StandardError);
		Assert.Equal(expectedOutput, result.Output);
	}
}
