#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
	[InlineData("no-report")]
	[InlineData("hung-devices")]
	[InlineData("hung-name")]
	[InlineData("hung-boot")]
	[InlineData("hung-launcher")]
	[InlineData("hung-animation")]
	[InlineData("hung-cpu")]
	[InlineData("hung-response")]
	public async Task TimeoutBoundsEveryReadinessOperationAndAwaitsExactProcess(string mode)
	{
		File.WriteAllText(Path.Combine(root, "mode"), mode);
		var clock = Stopwatch.StartNew();
		var options = new List<string> { "--port", "5580", "--timeout", "4" };
		if (mode == "hung-animation")
			options.Add("--no-animations");
		if (mode == "hung-cpu")
			options.AddRange(["--cpu-threshold", "1"]);
		if (mode == "hung-response")
			options.AddRange(["--response-threshold", "1"]);
		var running = Start(options.ToArray());
		await TrackChildren();
		var result = await running;
		await TrackChildren();
		Assert.NotEqual(0, result.ExitCode);
		Assert.Equal("", result.Stdout);
		Assert.True(clock.Elapsed < TimeSpan.FromSeconds(15), result.Stderr);
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
				"hung-name" => "emu avd name",
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
	[InlineData(false)]
	[InlineData(true)]
	public async Task CancellationOrCallerDeathBeforeDiscoveryCleansOwnedProcess(bool abrupt)
	{
		File.WriteAllText(Path.Combine(root, "mode"), "no-report");
		using var cli = Launch(StartArguments("--timeout", "30"));
		var output = cli.StandardOutput.ReadToEndAsync();
		var errors = cli.StandardError.ReadToEndAsync();
		await TrackChildren();
		if (abrupt)
			cli.Kill();
		else
			Assert.Equal(0, kill(cli.Id, 15));
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		await cli.WaitForExitAsync(timeout.Token);
		await Until(() => children.All(p => p.HasExited));
		Assert.NotEqual(0, cli.ExitCode);
		await Task.WhenAll(output, errors);
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

	[UnixFact]
	public async Task UnexpectedEmulatorExitMakesStrictStopFail()
	{
		Success(await Start("--port", "5580"));
		await TrackChildren();
		var emulator = children.First(p => p.Id == Pid("emulator"));
		File.WriteAllText(Path.Combine(root, "exit-code"), "7");
		await Until(() => emulator.HasExited);
		var result = await Cli("avd", "stop", "--home", root, "--name", name, "--timeout", "5");
		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains(nameof(SdkToolFailedExitException), result.Stderr);
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

	static Process Launch(string[] args)
	{
		var assembly = typeof(AvdLifecycle_Tests).Assembly.Location;
		var info = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
		{
			UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
		};
		foreach (var arg in new[] { "exec", "--runtimeconfig", Path.ChangeExtension(assembly, ".runtimeconfig.json"),
			"--depsfile", Path.ChangeExtension(assembly, ".deps.json"), typeof(AvdStartCommand).Assembly.Location }.Concat(args))
			info.ArgumentList.Add(arg);
		var process = new Process { StartInfo = info };
		process.Start();
		return process;
	}

	static async Task<Result> Cli(params string[] args)
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
		foreach (var kind in new[] { "owner", "emulator", "adb" })
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

	void WriteExecutable(string folder, string content)
	{
		Directory.CreateDirectory(Path.Combine(root, folder));
		var path = Path.Combine(root, folder, folder == "emulator" ? "emulator" : "adb");
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
		  hung-devices:*devices*|hung-name:*avd*name*|hung-boot:*dev.bootcomplete*|hung-launcher:*dumpsys*|hung-animation:*settings*|hung-cpu:*loadavg*|hung-response:*ro.build.display.id*)
		    exec sleep 60 ;;
		  probe-fail:*getprop*) echo fake-probe-failure >&2; exit 7 ;;
		esac
		case "$probe" in
		  "devices -l")
		    printf 'List of devices attached\nemulator-5582 device model:Unrelated\nemulator-5580 device model:Owned\n' ;;
		  *"emu avd name") cat "$root/name"; echo OK ;;
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
