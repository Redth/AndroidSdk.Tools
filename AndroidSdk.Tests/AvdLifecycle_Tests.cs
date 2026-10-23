#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AndroidSdk.Tool;
using Xunit;

namespace AndroidSdk.Tests;

public sealed class AvdLifecycle_Tests : IAsyncLifetime
{
	readonly string root = Path.Combine(AppContext.BaseDirectory, "AvdLifecycleTests", Guid.NewGuid().ToString("N"));
	readonly string name = "Lifecycle_" + Guid.NewGuid().ToString("N");
	readonly List<Process> children = new();
	string Logs => Path.Combine(root, "logs");

	public Task InitializeAsync()
	{
		if (OperatingSystem.IsWindows())
			return Task.CompletedTask;
		Directory.CreateDirectory(root);
		File.WriteAllText(Path.Combine(root, "name"), name + "\n");
		WriteExecutable("emulator", EmulatorScript);
		WriteExecutable("platform-tools", AdbScript);
		return Task.CompletedTask;
	}

	public async Task DisposeAsync()
	{
		if (!Directory.Exists(root))
			return;
		try
		{
			if (children.Any(p => !p.HasExited))
				await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "3");
		}
		finally
		{
			foreach (var process in children)
			{
				using (process)
				{
					if (!process.HasExited)
					{
						process.Kill(entireProcessTree: true);
						using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
						await process.WaitForExitAsync(cleanup.Token);
					}
				}
			}
			Directory.Delete(root, true);
		}
	}

	[UnixFact]
	public async Task DetachedReadyReturnsExactIdentityAndKeepsBothLogsLiveUntilStrictStop()
	{
		var result = await Start("--port", "5580", "--show-kernel", "--no-window", "--no-snapshot", "--cores", "2");
		Success(result);
		using var json = JsonDocument.Parse(result.Stdout);
		var identity = json.RootElement;
		Assert.Equal(name, identity.GetProperty("name").GetString());
		Assert.Equal("emulator-5580", identity.GetProperty("serial").GetString());
		Assert.Equal(5580, identity.GetProperty("port").GetInt32());
		Assert.Equal("", result.Stderr);
		await TrackChildren();
		var emulator = children.First(p => p.Id == Pid("emulator"));
		Assert.False(emulator.HasExited);
		var stdout = identity.GetProperty("standardOutputPath").GetString()!;
		var stderr = identity.GetProperty("standardErrorPath").GetString()!;
		var outLength = new FileInfo(stdout).Length;
		var errLength = new FileInfo(stderr).Length;
		await Until(() => new FileInfo(stdout).Length > outLength && new FileInfo(stderr).Length > errLength);
		Assert.Contains("emulator-out", File.ReadAllText(stdout));
		Assert.Contains("emulator-err", File.ReadAllText(stderr));
		var arguments = File.ReadAllLines(Path.Combine(root, "emulator.args"));
		Assert.Contains("-show-kernel", arguments);
		Assert.Contains("-no-window", arguments);
		Assert.Contains("-no-snapshot", arguments);
		Assert.Equal("5580", arguments[Array.IndexOf(arguments, "-port") + 1]);
		Assert.Equal("2", arguments[Array.IndexOf(arguments, "-cores") + 1]);
		Assert.DoesNotContain("-s emulator-5582", File.ReadAllText(Path.Combine(root, "adb.calls")));
		Assert.DoesNotContain("ro.boot.qemu.avd_name", File.ReadAllText(Path.Combine(root, "adb.calls")));

		var inventory = await Cli("device", "list", "--home", root, "--format", "json");
		Success(inventory);
		using var devices = JsonDocument.Parse(inventory.Stdout);
		Assert.Contains(devices.RootElement.EnumerateArray(), d => d.GetProperty("serial").GetString() == "emulator-5580");

		Success(await Cli("avd", "stop", "--home", root, "--serial", "emulator-5580", "--timeout", "5"));
		Assert.True(emulator.HasExited);
		Assert.NotEqual(0, (await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "1")).ExitCode);
	}

	[UnixFact]
	public async Task StopChecksIdentityAndDuplicateOwnerCannotReplaceTheRunningEmulator()
	{
		Success(await Start("--port", "5580"));
		await TrackChildren();
		var pid = Pid("emulator");
		var mismatch = await Cli("avd", "stop", "--home", root, "--name", name, "--serial", "emulator-5582", "--timeout", "2");
		Assert.NotEqual(0, mismatch.ExitCode);
		Assert.Contains("does not match", mismatch.Stderr);
		var duplicate = await Start("--port", "5580");
		Assert.NotEqual(0, duplicate.ExitCode);
		Assert.Equal(pid, Pid("emulator"));
		Assert.False(children.First(p => p.Id == pid).HasExited);
		Success(await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "5"));
	}

	[UnixTheory]
	[InlineData("null")]
	[InlineData("")]
	[InlineData(" \t")]
	[InlineData("{")]
	public async Task InvalidStopDataCannotTerminateTheOwnerOrPreventAValidStop(string request)
	{
		Success(await Start("--port", "5580"));
		await TrackChildren();
		var key = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\n" +
			Path.TrimEndingDirectorySeparator(root) + "\nname\n" + name;
		var endpoint = "av" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).Substring(0, 32);
		using (var pipe = new NamedPipeClientStream(".", endpoint, PipeDirection.InOut, PipeOptions.Asynchronous))
		{
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await pipe.ConnectAsync(timeout.Token);
			using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
			await writer.WriteLineAsync(request).WaitAsync(timeout.Token);
			using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
			Assert.Null(await reader.ReadLineAsync().WaitAsync(timeout.Token));
		}
		Assert.All(children.Where(p => p.Id == Pid("owner") || p.Id == Pid("emulator")), p => Assert.False(p.HasExited));
		Success(await Cli("avd", "stop", "--home", root, "--name", name, "--serial", "emulator-5580", "--timeout", "5"));
		await Until(() => children.All(p => p.HasExited));
	}

	[UnixTheory]
	[InlineData("large-probe-fail")]
	[InlineData("large-identity-fail")]
	public async Task LargeOwnerDiagnosticsCannotBlockFailureCleanup(string mode)
	{
		File.WriteAllText(Path.Combine(root, "mode"), mode);
		var clock = Stopwatch.StartNew();
		var running = Start("--port", "5580", "--timeout", "4");
		await TrackChildren();
		var result = await running;
		Assert.NotEqual(0, result.ExitCode);
		Assert.Equal("", result.Stdout);
		Assert.Contains("large-probe-end", result.Stderr);
		Assert.DoesNotContain("cleanup could not be confirmed", result.Stderr);
		Assert.True(clock.Elapsed < TimeSpan.FromSeconds(15), result.Stderr);
		Assert.All(children, p => Assert.True(p.HasExited));
	}

	[UnixTheory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NativeLifecycleNeverRunsAnUnneededHungJdkProbe(bool startupTimeout)
	{
		WriteExecutable("jdk/bin", """
			#!/bin/bash
			root="$(cd "$(dirname "$0")/../.." && pwd)"
			echo called > "$root/javac.called"
			sleep 60
			""", "javac");
		if (startupTimeout)
			File.WriteAllText(Path.Combine(root, "mode"), "no-report");
		var running = Start("--port", "5580", "--timeout", "3");
		await TrackChildren();
		var result = await running;
		if (startupTimeout)
			Assert.NotEqual(0, result.ExitCode);
		else
		{
			Success(result);
			Success(await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "1"));
		}
		Assert.False(File.Exists(Path.Combine(root, "javac.called")));
		await Until(() => children.All(p => p.HasExited));
	}

	[UnixTheory]
	[InlineData("no-report")]
	[InlineData("hung-devices")]
	[InlineData("hung-output-drain")]
	[InlineData("hung-name")]
	[InlineData("hung-identity")]
	[InlineData("hung-boot")]
	[InlineData("hung-launcher")]
	[InlineData("hung-animation")]
	[InlineData("hung-cpu")]
	[InlineData("hung-response")]
	public async Task TimeoutBoundsEveryReadinessOperationAndAwaitsExactProcess(string mode)
	{
		File.WriteAllText(Path.Combine(root, "mode"), mode);
		var clock = Stopwatch.StartNew();
		var options = new List<string> { "--port", "5580", "--timeout", mode == "hung-output-drain" ? "2" : "4" };
		if (mode == "hung-animation")
			options.Add("--no-animations");
		if (mode == "hung-cpu")
			options.AddRange(["--cpu-threshold", "1"]);
		if (mode == "hung-response")
			options.AddRange(["--response-threshold", "1"]);
		var running = Start(options.ToArray());
		await TrackChildren();
		if (mode != "no-report")
		{
			await Until(() => File.Exists(Path.Combine(root, "descendant.pid")));
			await TrackChildren();
		}
		var result = await running;
		await TrackChildren();
		Assert.NotEqual(0, result.ExitCode);
		Assert.Equal("", result.Stdout);
		Assert.True(clock.Elapsed < TimeSpan.FromSeconds(15), result.Stderr);
		if (mode == "hung-output-drain")
			Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), result.Stderr);
		Assert.Contains("AVD start failed", result.Stderr);
		Assert.True(children.First(p => p.Id == Pid("emulator")).HasExited, result.Stderr);
		Assert.All(children.Where(p => p.Id != Pid("owner")), p => Assert.True(p.HasExited));
		Assert.True(Directory.GetFiles(Logs, "*.stdout.log").Length == 1);
		Assert.True(Directory.GetFiles(Logs, "*.stderr.log").Length == 1);
		if (mode != "no-report")
		{
			var expectedProbe = mode switch
			{
				"hung-devices" => "devices -l",
				"hung-output-drain" => "devices -l",
				"hung-name" => "emu avd name",
				"hung-identity" => "getprop ro.boot.qemu.avd_name",
				"hung-boot" => "getprop dev.bootcomplete",
				"hung-launcher" => "dumpsys window displays",
				"hung-animation" => "settings put global",
				"hung-cpu" => "cat /proc/loadavg",
				_ => "getprop ro.build.display.id"
			};
			Assert.Contains(expectedProbe, File.ReadAllText(Path.Combine(root, "adb.calls")));
		}
	}

	[UnixTheory]
	[InlineData("probe-fail", false)]
	[InlineData("probe-fail", true)]
	[InlineData("exit0", true)]
	[InlineData("exit1", true)]
	public async Task StartupFailureCannotBecomeSuccessOrHangInWaitExit(string mode, bool attached)
	{
		File.WriteAllText(Path.Combine(root, "mode"), mode);
		var task = attached ? Start("--wait-exit", "--timeout", "4") : Start("--timeout", "4");
		await TrackChildren();
		var result = await task;
		Assert.NotEqual(0, result.ExitCode);
		Assert.Equal("", result.Stdout);
		Assert.Contains("AVD start failed", result.Stderr);
		Assert.All(children, p => Assert.True(p.HasExited));
		if (mode == "probe-fail")
			Assert.Contains("fake-probe-failure", result.Stderr);
	}

	[UnixTheory]
	[InlineData("no-report", false)]
	[InlineData("no-report", true)]
	[InlineData("hung-identity", false)]
	[InlineData("hung-identity", true)]
	public async Task CancellationOrCallerDeathDuringIdentityDiscoveryCleansOwnedProcess(string mode, bool abrupt)
	{
		File.WriteAllText(Path.Combine(root, "mode"), mode);
		using var cli = Launch(StartArguments("--timeout", "30"));
		var output = cli.StandardOutput.ReadToEndAsync();
		var errors = cli.StandardError.ReadToEndAsync();
		await TrackChildren();
		if (mode == "hung-identity")
		{
			await Until(() => File.Exists(Path.Combine(root, "descendant.pid")));
			await TrackChildren();
		}
		if (abrupt)
			cli.Kill();
		else
			Assert.Equal(0, kill(cli.Id, 15));
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		await cli.WaitForExitAsync(timeout.Token);
		await Until(() => children.All(p => p.HasExited));
		Assert.NotEqual(0, cli.ExitCode);
		await Task.WhenAll(output, errors);
		Assert.Equal("", await output);
		Assert.DoesNotContain("Unhandled exception.", await errors);
	}

	[UnixTheory]
	[InlineData("empty-name")]
	[InlineData("whitespace-name")]
	public async Task EmptyConsoleNameUsesExactGuestIdentityBeforeDetachedSuccess(string mode)
	{
		File.WriteAllText(Path.Combine(root, "mode"), mode);
		var result = await Start("--port", "5580");
		Success(result);
		using var json = JsonDocument.Parse(result.Stdout);
		Assert.Equal(name, json.RootElement.GetProperty("name").GetString());
		Assert.Equal("emulator-5580", json.RootElement.GetProperty("serial").GetString());
		Assert.Equal(5580, json.RootElement.GetProperty("port").GetInt32());
		await TrackChildren();
		Assert.False(children.First(p => p.Id == Pid("emulator")).HasExited);
		var calls = File.ReadAllText(Path.Combine(root, "adb.calls"));
		Assert.Contains("-s emulator-5580 emu avd name", calls);
		Assert.Contains("-s emulator-5580 shell getprop ro.boot.qemu.avd_name", calls);
		Assert.DoesNotContain("-s emulator-5582", calls);
		Success(await Cli("avd", "stop", "--home", root, "--name", name, "--serial", "emulator-5580", "--timeout", "5"));
		await Until(() => children.All(p => p.HasExited));
	}

	[UnixTheory]
	[InlineData("identity-mismatch")]
	[InlineData("identity-empty")]
	[InlineData("identity-fail")]
	[InlineData("console-mismatch")]
	public async Task MissingOrMismatchedIdentityCannotBecomeReady(string mode)
	{
		File.WriteAllText(Path.Combine(root, "mode"), mode);
		var running = Start("--port", "5580", "--timeout", "4");
		await TrackChildren();
		var result = await running;
		Assert.NotEqual(0, result.ExitCode);
		Assert.Equal("", result.Stdout);
		Assert.All(children, p => Assert.True(p.HasExited));
		var calls = File.ReadAllText(Path.Combine(root, "adb.calls"));
		Assert.DoesNotContain("-s emulator-5582", calls);
		Assert.DoesNotContain("getprop dev.bootcomplete", calls);
		if (mode == "console-mismatch")
			Assert.DoesNotContain("ro.boot.qemu.avd_name", calls);
		else
			Assert.Contains("-s emulator-5580 shell getprop ro.boot.qemu.avd_name", calls);
		if (mode == "identity-fail")
			Assert.Contains("fake-identity-failure", result.Stderr);
		else
			Assert.Contains("expected '" + name + "'", result.Stderr);
		Assert.DoesNotContain("Unhandled exception.", result.Stderr);
	}

	[UnixFact]
	public async Task BootFallbackAndAnrHandlingRemainAvailable()
	{
		File.WriteAllText(Path.Combine(root, "mode"), "fallback-anr");
		Success(await Start("--port", "5580", "--timeout", "10", "--no-animations"));
		await TrackChildren();
		var calls = File.ReadAllText(Path.Combine(root, "adb.calls"));
		Assert.Contains("getprop dev.bootcomplete", calls);
		Assert.Contains("getprop sys.boot_completed", calls);
		Assert.Contains("input keyevent KEYCODE_BACK", calls);
		Assert.Contains("settings put global animator_duration_scale 0", calls);
		Success(await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "5"));
	}

	[UnixFact]
	public async Task StopOnlyTerminatesItsOwnedProcess()
	{
		using var unrelated = Process.Start(new ProcessStartInfo("/bin/sleep", "60") { UseShellExecute = false })!;
		try
		{
			Success(await Start("--port", "5580"));
			await TrackChildren();
			Success(await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "5"));
			Assert.False(unrelated.HasExited);
		}
		finally
		{
			unrelated.Kill();
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await unrelated.WaitForExitAsync(timeout.Token);
		}
	}

	[UnixFact]
	public async Task LogOpenFailureIsNotSuccessAndDoesNotStartAnEmulator()
	{
		File.WriteAllText(Logs, "not a directory");
		var result = await Start();
		Assert.NotEqual(0, result.ExitCode);
		Assert.False(File.Exists(Path.Combine(root, "emulator.pid")));
	}

	[UnixFact]
	public async Task ASecondOwnerCannotClaimTheSameSerial()
	{
		Success(await Start("--port", "5580"));
		await TrackChildren();
		var original = children.First(p => p.Id == Pid("emulator"));
		File.WriteAllText(Path.Combine(root, "name"), name + "_other\n");
		var other = await Cli("avd", "start", "--home", root, "--name", name + "_other", "--port", "5580",
			"--wait", "--timeout", "10", "--format", "json", "--log-directory", Logs);
		Assert.NotEqual(0, other.ExitCode);
		Assert.False(original.HasExited);
		Success(await Cli("avd", "stop", "--home", root, "--serial", "emulator-5580", "--timeout", "5"));
		Assert.True(original.HasExited);
	}

	[UnixTheory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(7)]
	public async Task UnexpectedEmulatorExitMakesStrictStopFail(int exitCode)
	{
		Success(await Start("--port", "5580"));
		await TrackChildren();
		var emulator = children.First(p => p.Id == Pid("emulator"));
		File.WriteAllText(Path.Combine(root, "exit-code"), exitCode.ToString());
		await Until(() => emulator.HasExited);
		var result = await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "5");
		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("exited unexpectedly with code " + exitCode, result.Stderr);
	}

	[UnixTheory]
	[InlineData(0)]
	[InlineData(1)]
	public async Task AttachedReadyExitStillAcceptsNormalEmulatorExitCodes(int exitCode)
	{
		var startupPipe = "as" + Guid.NewGuid().ToString("N");
		using var bootstrap = new NamedPipeServerStream(startupPipe, PipeDirection.InOut, 1,
			PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
		using var owner = Launch(["--avd-session-host", startupPipe]);
		var output = owner.StandardOutput.ReadToEndAsync();
		var errors = owner.StandardError.ReadToEndAsync();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		await bootstrap.WaitForConnectionAsync(timeout.Token);
		using var writer = new StreamWriter(bootstrap, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
		using var reader = new StreamReader(bootstrap, Encoding.UTF8, leaveOpen: true);
		await writer.WriteLineAsync(JsonSerializer.Serialize(new AvdStartCommandSettings
		{
			Name = name, Home = root, Port = 5580, WaitForBoot = true, WaitForExit = true, Timeout = 5, LogDirectory = Logs
		})).WaitAsync(timeout.Token);
		using var identity = JsonDocument.Parse((await reader.ReadLineAsync().WaitAsync(timeout.Token))!);
		Assert.Equal("emulator-5580", identity.RootElement.GetProperty("Identity").GetProperty("serial").GetString());
		await writer.WriteLineAsync("\"attach\"").WaitAsync(timeout.Token);
		using var acknowledged = JsonDocument.Parse((await reader.ReadLineAsync().WaitAsync(timeout.Token))!);
		Assert.Equal(JsonValueKind.Null, acknowledged.RootElement.GetProperty("Error").ValueKind);
		await TrackChildren();
		File.WriteAllText(Path.Combine(root, "exit-code"), exitCode.ToString());
		using var completed = JsonDocument.Parse((await reader.ReadLineAsync().WaitAsync(timeout.Token))!);
		Assert.Equal(JsonValueKind.Null, completed.RootElement.GetProperty("Error").ValueKind);
		await owner.WaitForExitAsync(timeout.Token);
		Assert.Equal(0, owner.ExitCode);
		Assert.Equal("", await errors);
		Assert.Equal("", await output);
		Assert.All(children, p => Assert.True(p.HasExited));
	}

	[UnixFact]
	public async Task ReportedPortMustMatchTheRequestedPortBeforeAnyAdbProbe()
	{
		File.WriteAllText(Path.Combine(root, "mode"), "wrong-port");
		var running = Start("--port", "5580");
		await TrackChildren();
		var result = await running;
		Assert.NotEqual(0, result.ExitCode);
		Assert.Equal("", result.Stdout);
		Assert.Contains("different port", result.Stderr);
		Assert.False(File.Exists(Path.Combine(root, "adb.calls")));
		Assert.All(children, p => Assert.True(p.HasExited));
	}

	[UnixFact]
	public async Task ExitedLauncherCannotLeaveItsNativeChildRunning()
	{
		File.WriteAllText(Path.Combine(root, "mode"), "launcher-child");
		var running = Start("--timeout", "10");
		await Until(() => File.Exists(Path.Combine(root, "descendant.pid")));
		await TrackChildren();
		var descendant = children.First(p => p.Id == Pid("descendant"));
		Assert.False(descendant.HasExited);
		File.WriteAllText(Path.Combine(root, "release-launcher"), "");
		var result = await running;
		Assert.NotEqual(0, result.ExitCode);
		Assert.True(descendant.HasExited, result.Stderr);
		Assert.All(children, p => Assert.True(p.HasExited));
	}

	[UnixTheory]
	[InlineData("transient-devices")]
	[InlineData("transient-name")]
	[InlineData("identity-transient")]
	[InlineData("transient-launcher")]
	[InlineData("transient-response")]
	public async Task TransientDiscoveryLauncherAndResponseFailuresCanRecoverWithinTheBudget(string mode)
	{
		File.WriteAllText(Path.Combine(root, "mode"), mode);
		var result = mode == "transient-response"
			? await Start("--timeout", "30", "--response-threshold", "1")
			: await Start("--timeout", "10");
		Success(result);
		await TrackChildren();
		Assert.True(File.Exists(Path.Combine(root, "transient-failure")));
		if (mode == "transient-response")
			Assert.Equal(5, File.ReadAllLines(Path.Combine(root, "adb.calls")).Count(l => l.Contains("getprop ro.build.display.id")));
		Success(await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "5"));
	}

	[UnixFact]
	public void LauncherPhaseDeadlineIsBestEffortAndCancelsTheHungProbe()
	{
		File.WriteAllText(Path.Combine(root, "mode"), "hung-launcher");
		var process = new Emulator(root).Start(name);
		try
		{
			using var global = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			var clock = Stopwatch.StartNew();
			Assert.True(process.WaitForBootComplete(global.Token, "emulator-5580", TimeSpan.FromSeconds(1)));
			Assert.True(clock.Elapsed < TimeSpan.FromSeconds(7));
			Assert.False(global.IsCancellationRequested);
			Assert.False(process.HasExited);
			foreach (var kind in new[] { "adb", "descendant" })
			{
				try
				{
					using var child = Process.GetProcessById(Pid(kind));
					Assert.True(child.HasExited);
				}
				catch (ArgumentException) { }
			}
		}
		finally
		{
			process.Runner.KillAndWait();
			process.Runner.WaitForExit();
			process.Runner.Dispose();
		}
	}

	[UnixTheory]
	[InlineData("identity-transient")]
	[InlineData("transient-launcher")]
	[InlineData("transient-response")]
	public void RecoveredReadinessErrorsCannotContaminateLaterFailures(string mode)
	{
		File.WriteAllText(Path.Combine(root, "mode"), mode);
		var process = new Emulator(root).Start(name);
		try
		{
			using var global = new CancellationTokenSource(TimeSpan.FromSeconds(30));
			Assert.True(process.WaitForBootComplete(global.Token, "emulator-5580"));
			if (mode == "transient-response")
				Assert.True(process.WaitForStableResponses(2, 1, TimeSpan.Zero, global.Token));
			Assert.True(File.Exists(Path.Combine(root, "transient-failure")));
			Assert.Null(process.LastReadinessError);
			File.WriteAllText(Path.Combine(root, "mode"), "animation-fail");
			var failure = Assert.Throws<SdkToolFailedExitException>(() => process.DisableAnimations(global.Token));
			Assert.Contains("fake-animation-failure", failure.StdErr);
			Assert.Null(process.LastReadinessError);
		}
		finally
		{
			process.Runner.KillAndWait();
			process.Runner.WaitForExit();
			process.Runner.Dispose();
		}
	}

	[UnixFact]
	public async Task RequestedCpuAndResponseSettlingCompleteBeforeDetachedSuccess()
	{
		Success(await Start("--timeout", "30", "--cpu-threshold", "1", "--response-threshold", "1"));
		await TrackChildren();
		var calls = File.ReadAllLines(Path.Combine(root, "adb.calls"));
		Assert.Single(calls.Where(l => l.Contains("cat /proc/loadavg")));
		Assert.Equal(3, calls.Count(l => l.Contains("getprop ro.build.display.id")));
		Success(await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "5"));
	}

	[Fact]
	public async Task DiagnosticDisposalCannotWaitForAReadThatIgnoresCancellation()
	{
		using var reader = new BlockingDiagnosticReader();
		var diagnostics = new AvdSession.OwnerDiagnostics(reader);
		try
		{
			var clock = Stopwatch.StartNew();
			diagnostics.Dispose();
			Assert.True(clock.Elapsed < TimeSpan.FromSeconds(1));
			Assert.False(reader.IsDisposed);
		}
		finally
		{
			reader.EndRead.TrySetResult(0);
			await Until(() => reader.IsDisposed);
		}
	}

	[Fact]
	public async Task OwnerDiagnosticsRetainsOnlyABoundedTailWithoutNewlines()
	{
		using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(new string('x', 131072) + "diagnostic-end")));
		using var diagnostics = new AvdSession.OwnerDiagnostics(reader);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var tail = await diagnostics.CompleteAsync(timeout.Token);
		Assert.Equal(16384, tail.Length);
		Assert.EndsWith("diagnostic-end", tail);
	}

	sealed class BlockingDiagnosticReader : StreamReader
	{
		internal readonly TaskCompletionSource<int> EndRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
		internal volatile bool IsDisposed;

		internal BlockingDiagnosticReader() : base(Stream.Null) { }

		public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default) =>
			new(EndRead.Task);

		protected override void Dispose(bool disposing)
		{
			IsDisposed = true;
			base.Dispose(disposing);
		}
	}

	[Fact]
	public void LifecycleOptionsRejectUnboundedOrAmbiguousInputs()
	{
		Assert.False(new AvdStartCommandSettings { Name = name, Timeout = 0 }.Validate().Successful);
		Assert.False(new AvdStartCommandSettings { Name = name, Port = 5555 }.Validate().Successful);
		Assert.False(new AvdStartCommandSettings { Name = name, CpuThreshold = double.NaN }.Validate().Successful);
		Assert.False(new AvdStopCommandSettings().Validate().Successful);
		Assert.False(new AvdStopCommandSettings { Serial = "emulator-.*" }.Validate().Successful);
		Assert.False(new AvdStopCommandSettings { Name = name, Timeout = 0 }.Validate().Successful);
		Assert.True(new AvdStopCommandSettings { Serial = "emulator-5580" }.Validate().Successful);
	}

	string[] StartArguments(params string[] options) =>
		["avd", "start", "--home", root, "--name", name, "--wait", "--format", "json", "--log-directory", Logs,
			.. options.Contains("--timeout") ? Array.Empty<string>() : new[] { "--timeout", "10" }, .. options];

	Task<Result> Start(params string[] options) => Cli(StartArguments(options));

	void Success(Result result)
	{
		var diagnostics = "";
		if (result.ExitCode != 0)
			foreach (var path in Directory.GetFiles(root, "*", SearchOption.AllDirectories).Where(p => p.EndsWith(".log") || p.EndsWith(".calls") || p.EndsWith(".args")))
				diagnostics += "\n" + path + "\n" + File.ReadAllText(path);
		Assert.True(result.ExitCode == 0, result.Stdout + result.Stderr + diagnostics);
	}

	sealed record Result(int ExitCode, string Stdout, string Stderr);

	Process Launch(string[] args)
	{
		var assembly = typeof(AvdLifecycle_Tests).Assembly.Location;
		var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
		{
			UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
		};
		if (Directory.Exists(Path.Combine(root, "jdk")))
		{
			info.Environment["JAVA_HOME"] = Path.Combine(root, "jdk");
			info.Environment["JDK_HOME"] = Path.Combine(root, "jdk");
		}
		foreach (var arg in new[] { "exec", "--runtimeconfig", Path.ChangeExtension(assembly, ".runtimeconfig.json"),
			"--depsfile", Path.ChangeExtension(assembly, ".deps.json"), typeof(AvdStartCommand).Assembly.Location }.Concat(args))
			info.ArgumentList.Add(arg);
		var process = new Process { StartInfo = info };
		process.Start();
		return process;
	}

	async Task<Result> Cli(params string[] args)
	{
		using var process = Launch(args);
		var stdout = process.StandardOutput.ReadToEndAsync();
		var stderr = process.StandardError.ReadToEndAsync();
		try
		{
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(35));
			await process.WaitForExitAsync(timeout.Token);
			await Task.WhenAll(stdout, stderr).WaitAsync(timeout.Token);
			return new Result(process.ExitCode, await stdout, await stderr);
		}
		finally
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
				using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
				await process.WaitForExitAsync(cleanup.Token);
			}
		}
	}

	int Pid(string kind) => int.Parse(File.ReadAllText(Path.Combine(root, kind + ".pid")));

	async Task TrackChildren()
	{
		await Until(() => new[] { "owner", "emulator" }.All(kind =>
			File.Exists(Path.Combine(root, kind + ".pid")) && int.TryParse(File.ReadAllText(Path.Combine(root, kind + ".pid")), out _)));
		foreach (var kind in new[] { "owner", "emulator", "adb", "descendant" })
		{
			var path = Path.Combine(root, kind + ".pid");
			if (!File.Exists(path))
				continue;
			try { children.Add(Process.GetProcessById(Pid(kind))); }
			catch (ArgumentException) { }
		}
	}

	static async Task Until(Func<bool> condition)
	{
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
		while (!condition())
			await Task.Delay(50, timeout.Token);
	}

	void WriteExecutable(string folder, string content, string? executable = null)
	{
		Directory.CreateDirectory(Path.Combine(root, folder));
		var path = Path.Combine(root, folder, executable ?? (folder == "emulator" ? "emulator" : "adb"));
		File.WriteAllText(path, content);
		if (!OperatingSystem.IsWindows())
			File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
	}

	[DllImport("libc", SetLastError = true)]
	static extern int kill(int pid, int signal);

	public sealed class UnixFactAttribute : FactAttribute
	{
		public UnixFactAttribute()
		{
			if (OperatingSystem.IsWindows())
				Skip = "Uses fake Bash emulator/ADB executables; no Android SDK is needed.";
		}
	}

	public sealed class UnixTheoryAttribute : TheoryAttribute
	{
		public UnixTheoryAttribute()
		{
			if (OperatingSystem.IsWindows())
				Skip = "Uses fake Bash emulator/ADB executables; no Android SDK is needed.";
		}
	}

	const string EmulatorScript = """
		#!/bin/bash
		root="$(cd "$(dirname "$0")/.." && pwd)"
		echo $$ > "$root/emulator.pid"
		echo $PPID > "$root/owner.pid"
		printf '%s\n' "$@" > "$root/emulator.args"
		mode="$(cat "$root/mode" 2>/dev/null)"
		echo emulator-out-start
		echo emulator-err-start >&2
		case "$mode" in
		  exit0) exit 0 ;;
		  exit1) exit 1 ;;
		  launcher-child)
		    sleep 60 &
		    echo $! > "$root/descendant.pid"
		    while [ ! -f "$root/release-launcher" ]; do sleep 0.1; done
		    exit 0 ;;
		esac
		port=5580
		report=
		while [ $# -gt 0 ]; do
		  case "$1" in
		    -port) shift; port="$1" ;;
		    -report-console) shift; report="${1#tcp:}"; report="${report%%,*}" ;;
		  esac
		  shift
		done
		if [ -n "$report" ] && [ "$mode" != no-report ]; then
		  if [ "$mode" = wrong-port ]; then port=5582; fi
		  printf '%s\n' "$port" > "/dev/tcp/127.0.0.1/$report" || exit 8
		fi
		while true; do
		  if [ -f "$root/exit-code" ]; then exit "$(cat "$root/exit-code")"; fi
		  echo emulator-out-tick
		  echo emulator-err-tick >&2
		  sleep 0.2
		done
		""";

	const string AdbScript = """
		#!/bin/bash
		root="$(cd "$(dirname "$0")/.." && pwd)"
		mode="$(cat "$root/mode" 2>/dev/null)"
		echo $$ > "$root/adb.pid"
		printf '%s\n' "$*" >> "$root/adb.calls"
		probe="$*"
		case "$mode:$probe" in
		  hung-output-drain:*devices*)
		    sleep 60 &
		    echo $! > "$root/descendant.pid"
		    exit 0 ;;
		  hung-devices:*devices*|hung-name:*avd*name*|hung-identity:*ro.boot.qemu.avd_name*|hung-boot:*dev.bootcomplete*|hung-launcher:*dumpsys*|hung-animation:*settings*|hung-cpu:*loadavg*|hung-response:*ro.build.display.id*)
		    sleep 60 &
		    echo $! > "$root/descendant.pid"
		    wait $! ;;
		  probe-fail:*getprop*) echo fake-probe-failure >&2; exit 7 ;;
		  animation-fail:*settings*) echo fake-animation-failure >&2; exit 7 ;;
		  large-probe-fail:*getprop*|large-identity-fail:*ro.boot.qemu.avd_name*)
		    printf 'large-probe-start' >&2
		    head -c 131072 /dev/zero | tr '\000' x >&2
		    echo large-probe-end >&2
		    exit 7 ;;
		esac
		case "$mode:$probe" in
		  transient-devices:*devices*|transient-name:*avd*name*|identity-transient:*ro.boot.qemu.avd_name*|transient-launcher:*dumpsys*)
		    if [ ! -f "$root/transient-failure" ]; then
		      touch "$root/transient-failure"
		      echo fake-transient-failure >&2
		      exit 7
		    fi ;;
		  transient-response:*ro.build.display.id*)
		    count="$(cat "$root/response-count" 2>/dev/null)"
		    count=$((count + 1))
		    echo "$count" > "$root/response-count"
		    if [ "$count" -eq 2 ]; then
		      touch "$root/transient-failure"
		      echo fake-transient-failure >&2
		      exit 7
		    fi ;;
		esac
		case "$probe" in
		  "devices -l")
		    printf 'List of devices attached\nemulator-5582 device model:Unrelated\nemulator-5580 device model:Owned\n' ;;
		  *"emu avd name")
		    case "$mode" in
		      empty-name|identity-*|hung-identity|large-identity-fail) ;;
		      whitespace-name) printf ' \t\n' ;;
		      console-mismatch) echo unrelated-avd; echo OK ;;
		      *) cat "$root/name"; echo OK ;;
		    esac ;;
		  *"getprop ro.boot.qemu.avd_name")
		    case "$mode" in
		      identity-mismatch) echo unrelated-avd ;;
		      identity-empty) echo ;;
		      identity-fail) echo fake-identity-failure >&2; exit 7 ;;
		      *) cat "$root/name" ;;
		    esac ;;
		  *"getprop dev.bootcomplete") if [ "$mode" = fallback-anr ]; then echo 0; else echo 1; fi ;;
		  *"getprop sys.boot_completed") echo 1 ;;
		  *"dumpsys window displays")
		    if [ "$mode" = fallback-anr ] && [ ! -f "$root/dismissed" ]; then
		      echo 'mCurrentFocus Application Not Responding'
		    else
		      echo 'mCurrentFocus com.android.launcher'
		    fi ;;
		  *"input keyevent KEYCODE_BACK") touch "$root/dismissed" ;;
		  *"cat /proc/loadavg") echo '0.1 0.1 0.1 1/100 42' ;;
		  *"getprop ro.build.display.id") echo fake-build ;;
		  *"settings put global"*) ;;
		  *) echo "unexpected fake adb probe: $probe" >&2; exit 9 ;;
		esac
		""";
}
