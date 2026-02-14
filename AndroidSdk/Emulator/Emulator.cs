using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Data;

namespace AndroidSdk
{
	public partial class Emulator : SdkTool
	{
		public Emulator()
			: this((DirectoryInfo)null)
		{ }

		public Emulator(DirectoryInfo androidSdkHome)
			: base(androidSdkHome)
		{
		}

		public Emulator(string androidSdkHome)
			: this(string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome))
		{ }

		public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
			=> FindTool(androidSdkHome, toolName: "emulator", windowsExtension: ".exe", "emulator");

		public IEnumerable<string> ListAvds()
		{
			var builder = new ProcessArgumentBuilder();

			builder.Append("-list-avds");

			var p = Run(builder);

			foreach (var l in p)
			{
				if (!string.IsNullOrWhiteSpace(l))
					yield return l;
			}
		}

		public AndroidEmulatorProcess Start(string avdName, EmulatorStartOptions options = null)
		{
			if (options == null)
				options = new EmulatorStartOptions();

			var builder = new ProcessArgumentBuilder();

			builder.Append($"-avd {avdName}");

			if (options.NoSnapshotLoad)
				builder.Append("-no-snapshot-load");
			if (options.NoSnapshotSave)
				builder.Append("-no-snapshot-save");
			if (options.NoSnapshot)
				builder.Append("-no-snapshot");

			if (!string.IsNullOrEmpty(options.CameraBack))
				builder.Append($"-camera-back {options.CameraBack}");
			if (!string.IsNullOrEmpty(options.CameraFront))
				builder.Append($"-camera-front {options.CameraFront}");

			if (options.MemoryMegabytes.HasValue)
				builder.Append($"-memory {options.MemoryMegabytes}");

			if (options.PartitionSizeMegabytes.HasValue)
				builder.Append($"-partition-size {options.PartitionSizeMegabytes}");

			if (options.CacheSizeMegabytes.HasValue)
				builder.Append($"-cache-size {options.CacheSizeMegabytes}");

			if (options.SdCard != null)
			{
				builder.Append("-sdcard");
				builder.AppendQuoted(options.SdCard.FullName);
			}

			if (options.WipeData)
				builder.Append("-wipe-data");

			if (options.Debug != null && options.Debug.Length > 0)
				builder.Append("-debug " + string.Join(",", options.Debug));

			if (options.Logcat != null && options.Logcat.Length > 0)
				builder.Append("-logcat " + string.Join(",", options.Logcat));

			if (options.ShowKernel)
				builder.Append("-show-kernel");

			if (options.Verbose)
				builder.Append("-verbose");

			if (options.DnsServers != null && options.DnsServers.Length > 0)
				builder.Append("-dns-server " + string.Join(",", options.DnsServers));

			if (!string.IsNullOrEmpty(options.HttpProxy))
				builder.Append($"-http-proxy {options.HttpProxy}");

			if (!string.IsNullOrEmpty(options.NetDelay))
				builder.Append($"-netdelay {options.NetDelay}");

			if (options.NetFast)
				builder.Append("-netfast");

			if (!string.IsNullOrEmpty(options.NetSpeed))
				builder.Append($"-netspeed {options.NetSpeed}");

			if (options.Ports.HasValue)
				builder.Append($"-ports {options.Ports.Value.console},{options.Ports.Value.adb}");
			else if (options.Port.HasValue)
				builder.Append($"-port {options.Port.Value}");

			if (options.TcpDump != null)
			{
				builder.Append("-tcpdump");
				builder.AppendQuoted(options.TcpDump.FullName);
			}

			if (options.Acceleration.HasValue)
				builder.Append($"-accel {options.Acceleration.Value.ToString().ToLowerInvariant()}");

			if (options.NoAccel)
				builder.Append("-no-accel");

			if (options.Engine.HasValue)
				builder.Append($"-engine {options.Engine.Value.ToString().ToLowerInvariant()}");
			
			if (!string.IsNullOrEmpty(options.Gpu))
				builder.Append($"-gpu {options.Gpu.ToLowerInvariant()}");
			else
			{
				// Auto-detect GPU mode based on OS
				var autoGpu = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
					? "swiftshader_indirect"
					: "guest";
				builder.Append($"-gpu {autoGpu}");
			}

			if (options.NoWindow)
				builder.Append("-no-window");

			if (options.NoAudio)
				builder.Append("-no-audio");

			if (options.NoJni)
				builder.Append("-no-jni");

			if (options.SeLinux.HasValue)
				builder.Append($"-selinux {options.SeLinux.Value.ToString().ToLowerInvariant()}");

			if (!string.IsNullOrEmpty(options.Timezone))
				builder.Append($"-timezone {options.Timezone}");

			if (options.NoBootAnim)
				builder.Append("-no-boot-anim");

			if (options.Screen.HasValue)
				builder.Append($"-screen {options.Screen.Value.ToString().ToLowerInvariant()}");

			if (options.GrpcPort.HasValue)
				builder.Append($"-grpc {options.GrpcPort.Value}");

			if (options.GrpcUseJwt.HasValue)
				builder.Append("-grpc-use-jwt");

			//var uuid = Guid.NewGuid().ToString("D");
			//builder.Append($"-prop emu.uuid={uuid}");

			if (options.ExtraArgs != null && options.ExtraArgs.Length > 0)
			{
				foreach (var arg in options.ExtraArgs)
					builder.Append(arg);
			}

			return new AndroidEmulatorProcess(Start(builder), avdName, AndroidSdkHome);
		}

		IEnumerable<string> Run(ProcessArgumentBuilder builder, params string[] args)
		{
			var p = Start(builder, args);

			var r = p.WaitForExit();

			return r.StandardOutput;
		}

		ProcessRunner Start(ProcessArgumentBuilder builder, params string[] args)
		{
			var emulator = FindToolPath(AndroidSdkHome);
			if (emulator == null || !File.Exists(emulator.FullName))
				throw new FileNotFoundException("Could not find emulator", emulator?.FullName);

			if (args != null && args.Length > 0)
			{
				foreach (var arg in args)
					builder.Append(arg);
			}

			var p = new ProcessRunner(emulator, builder);
			return p;
		}

		public class AndroidEmulatorProcess
		{
			internal AndroidEmulatorProcess(ProcessRunner p, string avdName, DirectoryInfo sdkHome)
			{
				process = p;
				androidSdkHome = sdkHome;
				AvdName = avdName;
			}

			readonly ProcessRunner process;
			readonly DirectoryInfo androidSdkHome;
			ProcessResult result;

			public string Serial { get; private set; }

			public string AvdName { get; private set; }

			public int WaitForExit()
			{
				result = process.WaitForExit();

				if (result.ExitCode != 0 && result.ExitCode != 1)
					throw new SdkToolFailedExitException("emulator", result.ExitCode, result.StandardError, result.StandardOutput);

				return result.ExitCode;
			}

			public bool Shutdown()
			{
				var success = false;

				if (!string.IsNullOrWhiteSpace(Serial))
				{
					var adb = new Adb(androidSdkHome);

					try { success = adb.EmuKill(Serial); }
					catch { }
				}

				if (process != null && !process.HasExited)
				{
					try { process.Kill(); success = true; }
					catch { }
				}

				return success;
			}

			public IEnumerable<string> GetStandardOutput()
				=> process.StandardOutput ?? new List<string>();
			
			public IEnumerable<string> GetStandardError()
				=> process.StandardError ?? new List<string>();

			public IEnumerable<string> GetOutput()
				=> process.Output ?? new List<string>();

			public bool WaitForBootComplete()
				=> WaitForBootComplete(TimeSpan.Zero);

			public bool WaitForBootComplete(TimeSpan timeout)
			{
				var cts = new CancellationTokenSource();
				
				if (timeout != TimeSpan.Zero)
					cts.CancelAfter(timeout);

				return WaitForBootComplete(cts.Token);
			}

			public bool WaitForBootComplete(CancellationToken token)
			{
				var adb = new Adb(androidSdkHome);

				var booted = false;
				Serial = null;

				// Phase 1: Find the emulator device in adb
				while (string.IsNullOrEmpty(Serial) && !token.IsCancellationRequested)
				{
					if (process.HasExited)
						return false;

					Thread.Sleep(1000);

					var devices = adb.GetDevices();

					foreach (var d in devices)
					{
						try
						{
							var name = adb.GetEmulatorName(d.Serial);
							if (name.Equals(AvdName, StringComparison.OrdinalIgnoreCase))
							{
								Serial = d.Serial;
								break;
							}
						}
						catch { }
					}
				}

				// Phase 2: Wait for boot_completed property
				while (!token.IsCancellationRequested)
				{
					if (process.HasExited)
						return false;

					if (adb.Shell("getprop dev.bootcomplete", Serial).Any(l => l.Contains("1")) ||
					    adb.Shell("getprop sys.boot_completed", Serial).Any(l => l.Contains("1")))
					{
						booted = true;
						break;
					}
					else
					{
						Thread.Sleep(1000);
					}
				}

				if (!booted)
					return false;

				// Phase 3: Wait for system services to settle
				WaitForSystemSettle(adb, token);

				// Phase 4: Prepare emulator for testing
				PrepareForTesting(adb);

				return true;
			}

			void WaitForSystemSettle(Adb adb, CancellationToken token)
			{
				// Wait for launcher/home to be in the foreground
				var launcherAttempts = 0;
				while (launcherAttempts < 30 && !token.IsCancellationRequested)
				{
					try
					{
						var focusLines = adb.Shell("dumpsys window displays", Serial);
						var focusText = string.Join("\n", focusLines);
						if (focusText.Contains("Launcher") || focusText.Contains("launcher") ||
						    focusText.Contains("HomeActivity") || focusText.Contains("home"))
							break;
					}
					catch { }

					launcherAttempts++;
					Thread.Sleep(2000);
				}

				// Wait for CPU load to drop below threshold
				var loadAttempts = 0;
				while (loadAttempts < 30 && !token.IsCancellationRequested)
				{
					try
					{
						var loadLines = adb.Shell("cat /proc/loadavg", Serial);
						if (loadLines.Count > 0)
						{
							var parts = loadLines[0].Split(' ');
							if (parts.Length > 0 && double.TryParse(parts[0],
								System.Globalization.NumberStyles.Float,
								System.Globalization.CultureInfo.InvariantCulture,
								out var load1Min) && load1Min < 3.0)
								break;
						}
					}
					catch { }

					loadAttempts++;
					Thread.Sleep(2000);
				}

				// Final settling pause
				Thread.Sleep(10000);
			}

			void PrepareForTesting(Adb adb)
			{
				// Disable animations for more reliable UI testing
				try { adb.Shell("settings put global window_animation_scale 0", Serial); } catch { }
				try { adb.Shell("settings put global transition_animation_scale 0", Serial); } catch { }
				try { adb.Shell("settings put global animator_duration_scale 0", Serial); } catch { }

				// Dismiss lock screen
				try { adb.Shell("input keyevent 82", Serial); } catch { }

				// Increase logcat buffer for better diagnostics
				try { adb.Shell("logcat -G 16M", Serial); } catch { }
			}
		}
	}
}
