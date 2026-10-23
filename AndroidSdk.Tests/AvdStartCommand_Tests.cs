#nullable enable
using System;
using System.IO;
using AndroidSdk.Tool;
using Spectre.Console.Cli;
using Xunit;

namespace AndroidSdk.Tests;

public class AvdStartCommand_Tests : IDisposable
{
	readonly DirectoryInfo root = Directory.CreateDirectory(
		Path.Combine(AppContext.BaseDirectory, "AvdStartCommandTests", Guid.NewGuid().ToString()));

	public void Dispose() => root.Delete(true);

	[UnixTheory]
	[InlineData(false)]
	[InlineData(true)]
	public void ForwardsShowKernelToEmulatorOnlyWhenRequested(bool showKernel)
	{
		if (OperatingSystem.IsWindows())
			throw new PlatformNotSupportedException();

		var emulatorDirectory = Directory.CreateDirectory(Path.Combine(root.FullName, "emulator"));
		var emulatorPath = Path.Combine(emulatorDirectory.FullName, "emulator");
		File.WriteAllText(emulatorPath, "#!/bin/sh\nprintf '%s\\n' \"$@\" > \"$0.args\"\n");
		File.SetUnixFileMode(emulatorPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

		var app = new CommandApp<AvdStartCommand>();
		app.Configure(config => config.PropagateExceptions());
		var args = new[]
		{
			"--home", root.FullName,
			"--name", "TestDevice",
			"--log-directory", Path.Combine(root.FullName, "logs"),
			"--wait-exit"
		};
		if (showKernel)
			args = [.. args, "--show-kernel"];

		Assert.Equal(0, app.Run(args));

		var emulatorArgs = File.ReadAllLines(emulatorPath + ".args");
		Assert.Equal(showKernel ? new[] { "-avd", "TestDevice", "-show-kernel" } : new[] { "-avd", "TestDevice" }, emulatorArgs);
	}

	public sealed class UnixTheoryAttribute : TheoryAttribute
	{
		public UnixTheoryAttribute()
		{
			if (OperatingSystem.IsWindows())
				Skip = "Uses a stub executable with the Unix system shell, not a real Android emulator.";
		}
	}
}
