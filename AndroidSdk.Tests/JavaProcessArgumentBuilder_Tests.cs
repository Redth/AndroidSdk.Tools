using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class JavaProcessArgumentBuilder_Tests : TestsBase
{
	public JavaProcessArgumentBuilder_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void IsCorrect()
	{
		var args = new JavaProcessArgumentBuilder("com.example.JavaCli");
		args.Append("-a");
		args.Append("-b");
		args.AppendQuoted("c");
		args.SetEnvVar("TEST_ENV_VAR", "this is a value");
		args.SetWorkingDirectory("this/is/the/working/directory");
		args.AppendClassPath("path/to/jar.jar");
		args.AppendJavaToolOptions("-Dcom.example.hastools=true");

		Assert.Equal("com.example.JavaCli -a -b \"c\"", args.ToString());
		Assert.Equal("com.example.JavaCli", args.Package);
		Assert.Equal(new[] { "-a", "-b", "\"c\"" }, args.Args);
		Assert.Equal(
			new[]
			{
				("CLASSPATH", "path/to/jar.jar"),
				("JAVA_TOOL_OPTIONS", "-Dcom.example.hastools=true"),
				("TEST_ENV_VAR", "this is a value")
			},
			args.EnvVars.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)));
		Assert.Equal("this/is/the/working/directory", args.WorkingDirectory);
	}

	[Fact]
	public void IsClassPathCorrect()
	{
		var args = new JavaProcessArgumentBuilder("com.example.JavaCli");
		args.AppendClassPath("path/to/jar1.jar");
		args.AppendClassPath("path/to/jar2.jar");

		Assert.Equal(
			new[] { ("CLASSPATH", "path/to/jar1.jar" + Path.PathSeparator + "path/to/jar2.jar"), },
			args.EnvVars.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)));
	}

	[Fact]
	public void IsJavaToolOptionsCorrect()
	{
		var args = new JavaProcessArgumentBuilder("com.example.JavaCli");
		args.AppendJavaToolOptions("-Dcom.example.hastools=true");
		args.AppendJavaToolOptions("-Dcom.example.hasmore=true");

		Assert.Equal(
			new[] { ("JAVA_TOOL_OPTIONS", "-Dcom.example.hastools=true -Dcom.example.hasmore=true"), },
			args.EnvVars.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)));
	}

	[Fact]
	public void CanClone()
	{
		var old = new JavaProcessArgumentBuilder("com.example.JavaCli");
		old.Append("-a");
		old.Append("-b");
		old.AppendQuoted("c");
		old.SetEnvVar("TEST_ENV_VAR", "this is a value");
		old.SetWorkingDirectory("this/is/the/working/directory");
		old.AppendClassPath("path/to/jar.jar");
		old.AppendJavaToolOptions("-Dcom.example.hastools=true");

		var args = new JavaProcessArgumentBuilder(old);
		Assert.Equal("com.example.JavaCli -a -b \"c\"", args.ToString());
		Assert.Equal("com.example.JavaCli", args.Package);
		Assert.Equal(
			new[]
			{
				("CLASSPATH", "path/to/jar.jar"),
				("JAVA_TOOL_OPTIONS", "-Dcom.example.hastools=true"),
				("TEST_ENV_VAR", "this is a value")
			},
			args.EnvVars.OrderBy(p => p.Key).Select(p => (p.Key, p.Value)));
		Assert.Equal("this/is/the/working/directory", args.WorkingDirectory);
	}
}
