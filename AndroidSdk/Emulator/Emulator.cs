using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

		public bool StopAvd(string avdName, TimeSpan timeout)
		{
			if (string.IsNullOrWhiteSpace(avdName))
				throw new ArgumentException("AVD name must be provided", nameof(avdName));

			var adb = new Adb(AndroidSdkHome);
			var serial = FindRunningEmulatorSerialByAvdName(adb, avdName);
			if (string.IsNullOrWhiteSpace(serial))
				return false;

			adb.EmuKill(serial);

			var sw = System.Diagnostics.Stopwatch.StartNew();
			while (sw.Elapsed < timeout)
			{
				try
				{
					var stillRunning = adb.GetDevices().Any(d => d.Serial.Equals(serial, StringComparison.OrdinalIgnoreCase));
					if (!stillRunning)
						return true;
				}
				catch (SdkToolFailedExitException)
				{
					// adb can transiently fail during shutdown.
				}

				Thread.Sleep(250);
			}

			try
			{
				return !adb.GetDevices().Any(d => d.Serial.Equals(serial, StringComparison.OrdinalIgnoreCase));
			}
			catch (SdkToolFailedExitException)
			{
				return false;
			}
		}

		static string FindRunningEmulatorSerialByAvdName(Adb adb, string avdName)
		{
			IEnumerable<Adb.AdbDevice> devices;
			try
			{
				devices = adb.GetDevices();
			}
			catch (SdkToolFailedExitException)
			{
				// adb can transiently fail while the server/emulator is still coming up.
				return null;
			}

			foreach (var device in devices)
			{
				try
				{
					var runningName = adb.GetEmulatorName(device.Serial);
					if (string.Equals(runningName, avdName, StringComparison.OrdinalIgnoreCase))
						return device.Serial;
				}
				catch (InvalidDataException)
				{
					// Non-emulator devices are expected here.
				}
				catch (SdkToolFailedExitException)
				{
					// Some emulator instances may reject emu commands while shutting down.
				}
				catch (IOException)
				{
					// Emulator instances can transiently reject socket access while booting/shutting down.
				}
				catch (System.Net.Sockets.SocketException)
				{
					// Emulator instances can transiently reject socket access while booting/shutting down.
				}
			}

			return null;
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

			public bool HasExited
				=> process?.HasExited ?? true;

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
				using var cts = new CancellationTokenSource();

				if (timeout != TimeSpan.Zero)
					cts.CancelAfter(timeout);

				return WaitForBootComplete(cts.Token);
			}

			public bool WaitForBootComplete(CancellationToken token)
			{
				var adb = new Adb(androidSdkHome);

				Serial = null;

				while (string.IsNullOrEmpty(Serial) && !token.IsCancellationRequested)
				{
					if (process.HasExited)
						return false;

					Serial = FindRunningEmulatorSerialByAvdName(adb, AvdName);
					if (!string.IsNullOrEmpty(Serial))
						break;

					if (token.WaitHandle.WaitOne(1000))
						return false;
				}

				// Keep trying to see if the boot complete prop is set
				var booted = false;
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

					if (token.WaitHandle.WaitOne(1000))
						return false;
				}

				// Always wait for launcher after boot (like iOS waits for SpringBoard)
				while (booted && !token.IsCancellationRequested)
				{
					if (process.HasExited)
						break;

					var output = adb.Shell("dumpsys window displays", Serial);
					if (output.Any(l =>
						l.Contains("mCurrentFocus", StringComparison.OrdinalIgnoreCase) &&
						l.Contains("launcher", StringComparison.OrdinalIgnoreCase)))
					{
						break;
					}

					if (token.WaitHandle.WaitOne(1000))
						break;
				}

				return booted;
			}

			internal bool WaitForCpuLoadBelow(double threshold, TimeSpan timeout, TimeSpan settleDelay, CancellationToken token)
				=> WaitForCpuLoadBelow(threshold, timeout, settleDelay, token, out _);

			internal bool WaitForCpuLoadBelow(double threshold, TimeSpan timeout, TimeSpan settleDelay, CancellationToken token, out double lastLoad)
			{
				lastLoad = -1;

				if (string.IsNullOrWhiteSpace(Serial))
					return false;

				var adb = new Adb(androidSdkHome);

				using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
				cts.CancelAfter(timeout);

				while (!cts.IsCancellationRequested)
				{
					if (process.HasExited)
						return false;

					var line = adb.Shell("cat /proc/loadavg", Serial)?.FirstOrDefault();
					if (!string.IsNullOrWhiteSpace(line))
					{
						var first = line.Split([' '], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
						if (double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out var load))
						{
							lastLoad = load;
							if (load < threshold)
							{
								if (settleDelay > TimeSpan.Zero && cts.Token.WaitHandle.WaitOne(settleDelay))
									return false;

								return true;
							}
						}
					}

					if (cts.Token.WaitHandle.WaitOne(5000))
						return false;
				}

				return false;
			}

			public void DisableAnimations()
			{
				if (string.IsNullOrWhiteSpace(Serial))
					return;

				var adb = new Adb(androidSdkHome);
				adb.Shell("settings put global window_animation_scale 0", Serial);
				adb.Shell("settings put global transition_animation_scale 0", Serial);
				adb.Shell("settings put global animator_duration_scale 0", Serial);
			}
		}
	}
}
