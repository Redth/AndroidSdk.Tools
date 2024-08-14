using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class ProcessArgumentBuilder_Tests : TestsBase
{
	public ProcessArgumentBuilder_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void IsCorrect()
	{
		var args = new ProcessArgumentBuilder();
		args.Append("-a");
		args.Append("-b");
		args.AppendQuoted("c");
		args.SetEnvVar("TEST_ENV_VAR", "this is a value");
		args.SetWorkingDirectory("this/is/the/working/directory");

		Assert.Equal("-a -b \"c\"", args.ToString());
		Assert.Equal(new[] { "-a", "-b", "\"c\"" }, args.Args);
		Assert.Equal(new[] { ("TEST_ENV_VAR", "this is a value") }, args.EnvVars.Select(p => (p.Key, p.Value)));
		Assert.Equal("this/is/the/working/directory", args.WorkingDirectory);
	}

	[Fact]
	public void CanClone()
	{
		var old = new ProcessArgumentBuilder();
		old.Append("-a");
		old.Append("-b");
		old.AppendQuoted("c");
		old.SetEnvVar("TEST_ENV_VAR", "this is a value");
		old.SetWorkingDirectory("this/is/the/working/directory");

		var args = new ProcessArgumentBuilder(old);
		Assert.Equal("-a -b \"c\"", args.ToString());
		Assert.Equal(new[] { "-a", "-b", "\"c\"" }, args.Args);
		Assert.Equal(new[] { ("TEST_ENV_VAR", "this is a value") }, args.EnvVars.Select(p => (p.Key, p.Value)));
		Assert.Equal("this/is/the/working/directory", args.WorkingDirectory);
	}
}
