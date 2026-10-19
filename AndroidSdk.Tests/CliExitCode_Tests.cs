#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AndroidSdk.Tool;
using Spectre.Console.Cli;
using Xunit;

namespace AndroidSdk.Tests;

public class CliExitCode_Tests
{
	[Fact]
	public async Task UnknownCommandReturnsFailure()
	{
		var result = await RunCli("not-an-android-command");

		AssertFailure(result);
		Assert.Contains("not-an-android-command", result.Output);
	}

	[Theory]
	[InlineData("create", null, "--name")]
	[InlineData("create", "ExitCodeTest", "--sdkid")]
	[InlineData("delete", null, "--name")]
	public async Task InvalidAvdCommandReturnsFailure(string command, string? name, string missingOption)
	{
		var args = name == null
			? new[] { "avd", command }
			: new[] { "avd", command, "--name", name };
		var result = await RunCli(args);

		AssertFailure(result);
		Assert.Contains($"Missing {missingOption}", result.Output);
	}

	[Fact]
	public async Task HelpReturnsSuccess()
	{
		var result = await RunCli("--help");

		Assert.True(result.ExitCode == 0, result.Output);
		Assert.Contains("avd", result.Output);
	}

	[Fact]
	public async Task NonzeroHandlerStatusReachesProcessExitCode()
	{
		var missingHome = Path.Combine(AppContext.BaseDirectory, $"missing-jdk-{Guid.NewGuid():N}");
		Assert.False(Directory.Exists(missingHome));
		var app = new CommandApp<JdkDotNetPreferCommand>();
		var args = new[] { "--home", missingHome };

		// A specified missing JDK returns before updating any user configuration.
		Assert.Equal(1, app.Run(args));
		var result = await RunCli("jdk", "dotnet-prefer", "--home", missingHome);

		Assert.True(result.ExitCode == 1, result.Output);
	}

	[Fact]
	public async Task UnexpectedCommandExceptionReturnsFailure()
	{
		var directory = Path.Combine(AppContext.BaseDirectory, $"cli-exit-code-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		try
		{
			var package = Path.Combine(directory, "invalid.apk");
			await File.WriteAllTextAsync(package, "Not an APK archive");
			Assert.Throws<InvalidDataException>(() => new ApkManifestInfoCommand().Execute(
				null!, new ApkManifestInfoCommandSettings { Package = package }));

			var result = await RunCli("apk", "manifest", "info", "--package", package);

			AssertFailure(result);
			Assert.Contains("Error:", result.Output);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[UnixAvdFailureTheory]
	[InlineData("create")]
	[InlineData("delete")]
	public async Task AvdToolFailureReturnsHandlerFailure(string command)
	{
		if (OperatingSystem.IsWindows())
			throw new PlatformNotSupportedException();

		var directory = Path.Combine(AppContext.BaseDirectory, $"avd-exit-code-{Guid.NewGuid():N}");
		var jdk = Path.Combine(directory, "jdk");
		var sdk = Path.Combine(directory, "sdk");
		var javaBin = Path.Combine(jdk, "bin");
		var tools = Path.Combine(sdk, "cmdline-tools", "latest");
		var marker = $"fake-avd-failure-{Guid.NewGuid():N}";
		Directory.CreateDirectory(javaBin);
		try
		{
			Directory.CreateDirectory(Path.Combine(tools, "bin"));
			Directory.CreateDirectory(Path.Combine(tools, "lib"));
			await File.WriteAllTextAsync(Path.Combine(tools, "bin", "avdmanager"), "");
			var javac = Path.Combine(javaBin, "javac");
			var java = Path.Combine(javaBin, "java");
			await File.WriteAllTextAsync(javac, "#!/bin/sh\necho 'javac 99.0.0'\n");
			await File.WriteAllTextAsync(java, $"#!/bin/sh\necho '{marker}' >&2\nexit 7\n");
			var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
			File.SetUnixFileMode(javac, mode);
			File.SetUnixFileMode(java, mode);
			var args = command == "create"
				? new[] { "avd", command, "--home", sdk, "--name", "ExitCodeTest", "--sdk", "fake-system-image" }
				: new[] { "avd", command, "--home", sdk, "--name", "ExitCodeTest" };

			var result = await RunCli(args, jdk);

			Assert.Contains(marker, result.Output);
			Assert.Contains(nameof(SdkToolFailedExitException), result.Output);
			Assert.True(result.ExitCode == 1, result.Output);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	public sealed class UnixAvdFailureTheoryAttribute : TheoryAttribute
	{
		public UnixAvdFailureTheoryAttribute()
		{
			if (OperatingSystem.IsWindows())
				Skip = "The fake SDK/JDK executables require a Unix shell.";
		}
	}

	private static void AssertFailure((int ExitCode, string Output) result) =>
		Assert.True(result.ExitCode != 0, $"Expected a nonzero exit code. Output:\n{result.Output}");

	private static Task<(int ExitCode, string Output)> RunCli(params string[] args) =>
		RunCli(args, javaHome: null);

	private static async Task<(int ExitCode, string Output)> RunCli(string[] args, string? javaHome)
	{
		var testAssembly = typeof(CliExitCode_Tests).Assembly.Location;
		var startInfo = new ProcessStartInfo
		{
			FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		// Use the test runtime and dependencies to run the referenced CLI without installing it.
		startInfo.ArgumentList.Add("exec");
		startInfo.ArgumentList.Add("--runtimeconfig");
		startInfo.ArgumentList.Add(Path.ChangeExtension(testAssembly, ".runtimeconfig.json"));
		startInfo.ArgumentList.Add("--depsfile");
		startInfo.ArgumentList.Add(Path.ChangeExtension(testAssembly, ".deps.json"));
		startInfo.ArgumentList.Add(typeof(AvdCreateCommand).Assembly.Location);
		foreach (var arg in args)
			startInfo.ArgumentList.Add(arg);
		if (javaHome != null)
		{
			startInfo.Environment["JAVA_HOME"] = javaHome;
			startInfo.Environment.Remove("JDK_HOME");
		}

		using var process = new Process { StartInfo = startInfo };
		process.Start();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
		var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
		try
		{
			await Task.WhenAll(process.WaitForExitAsync(timeout.Token), stdout, stderr);
			return (process.ExitCode, await stdout + await stderr);
		}
		finally
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
				using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
				await process.WaitForExitAsync(cleanupTimeout.Token);
			}
		}
	}
}
