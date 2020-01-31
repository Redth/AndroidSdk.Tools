using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Android.Tool.Adb
{
	partial class Adb
	{
		public Adb(AdbToolSettings settings)
		{
			runner = new AdbToolRunner();
			Settings = settings;
		}

		public Adb(DirectoryInfo androidSdkHome)
			: this(new AdbToolSettings { AndroidSdkRoot = androidSdkHome })
		{
		}

		public Adb(DirectoryInfo androidSdkHome, string deviceSerial)
			: this(new AdbToolSettings { AndroidSdkRoot = androidSdkHome, Serial = deviceSerial })
		{
		}


		public Adb(string androidSdkHome)
		: this(new AdbToolSettings { AndroidSdkRoot = new DirectoryInfo(androidSdkHome) })
		{
		}

		public Adb(string androidSdkHome, string deviceSerial)
		: this(new AdbToolSettings { AndroidSdkRoot = new DirectoryInfo(androidSdkHome), Serial = deviceSerial })
		{
		}

		public AdbToolSettings Settings { get; set; }
		readonly AdbToolRunner runner;

		public List<AdbDeviceInfo> GetDevices()
		{
			var devices = new List<AdbDeviceInfo>();

			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			builder.Append("devices");
			builder.Append("-l");

			runner.RunAdb(Settings, builder, out var lines);

			if (lines != null && lines.Count > 1)
			{
				foreach (var line in lines?.Skip(1))
				{
					var parts = Regex.Split(line, "\\s+");

					var d = new AdbDeviceInfo
					{
						Serial = parts[0].Trim()
					};

					if (parts.Length > 1 && (parts[1]?.ToLowerInvariant() ?? "offline") == "offline")
						continue;

					if (parts.Length > 2)
					{
						foreach (var part in parts.Skip(2))
						{
							var bits = part.Split(new[] { ':' }, 2);
							if (bits == null || bits.Length != 2)
								continue;

							switch (bits[0].ToLower())
							{
								case "usb":
									d.Usb = bits[1];
									break;
								case "product":
									d.Product = bits[1];
									break;
								case "model":
									d.Model = bits[1];
									break;
								case "device":
									d.Device = bits[1];
									break;
							}
						}
					}

					if (!string.IsNullOrEmpty(d?.Serial))
						devices.Add(d);
				}
			}

			return devices;
		}

		public void KillServer()
		{
			//adb kill-server
			var builder = new ProcessArgumentBuilder();

			builder.Append("kill-server");

			runner.RunAdb(Settings, builder);
		}

		public void StartServer()
		{
			//adb kill-server
			var builder = new ProcessArgumentBuilder();

			builder.Append("start-server");

			runner.RunAdb(Settings, builder);
		}

		public void Connect(string deviceIp, int port = 5555)
		{
			// adb connect device_ip_address:5555
			var builder = new ProcessArgumentBuilder();

			builder.Append("connect");
			builder.Append(deviceIp + ":" + port);

			runner.RunAdb(Settings, builder);
		}

		public void Disconnect(string deviceIp = null, int? port = null)
		{
			// adb connect device_ip_address:5555
			var builder = new ProcessArgumentBuilder();

			builder.Append("disconnect");
			if (!string.IsNullOrEmpty(deviceIp))
				builder.Append(deviceIp + ":" + (port ?? 5555));

			runner.RunAdb(Settings, builder);
		}

		public void Install(FileInfo apkFile)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("install");
			builder.Append(apkFile.FullName);

			runner.RunAdb(Settings, builder);
		}

		public void WaitFor(AdbTransport transport = AdbTransport.Any, AdbState state = AdbState.Device)
		{
			// adb wait-for[-<transport>]-<state>
			//  transport: usb, local, or any (default)
			//  state: device, recovery, sideload, bootloader
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			var x = "wait-for";
			if (transport == AdbTransport.Local)
				x = "-local";
			else if (transport == AdbTransport.Usb)
				x = "-usb";

			switch (state)
			{
				case AdbState.Bootloader:
					x += "-bootloader";
					break;
				case AdbState.Device:
					x += "-device";
					break;
				case AdbState.Recovery:
					x += "-recovery";
					break;
				case AdbState.Sideload:
					x += "-sideload";
					break;
			}

			builder.Append(x);

			runner.RunAdb(Settings, builder);
		}

		public void Uninstall(string packageName, bool keepDataAndCacheDirs = false)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("uninstall");
			if (keepDataAndCacheDirs)
				builder.Append("-k");
			builder.Append(packageName);

			runner.RunAdb(Settings, builder);
		}

		public bool EmuKill()
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("emu");
			builder.Append("kill");

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);

			return output != null && output.Any(o => o.ToLowerInvariant().Contains("stopping emulator"));
		}

		public bool Pull(FileInfo remoteFileSource, FileInfo localFileDestination)
			=> pull(remoteFileSource.FullName, localFileDestination.FullName);


		public bool Pull(DirectoryInfo remoteDirectorySource, DirectoryInfo localDirectoryDestination)
			=> pull(remoteDirectorySource.FullName, localDirectoryDestination.FullName);
	
		public bool Pull(FileInfo remoteFileSource, DirectoryInfo localDirectoryDestination)
			=> pull(remoteFileSource.FullName, localDirectoryDestination.FullName);

		bool pull(string remoteSrc, string localDest)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("pull");
			builder.AppendQuoted(remoteSrc);
			builder.AppendQuoted(localDest);

			return runner.RunAdb(Settings, builder);
		}

		public bool Push(FileInfo localFileSource, FileInfo remoteFileDestination)
			=> push(localFileSource.FullName, remoteFileDestination.FullName);

		public bool Push(FileInfo localFileSource, DirectoryInfo remoteDirectoryDestination)
			=>push(localFileSource.FullName, remoteDirectoryDestination.FullName);
		
		public bool Push(DirectoryInfo localDirectorySource, DirectoryInfo remoteDirectoryDestination)
			=> push(localDirectorySource.FullName, remoteDirectoryDestination.FullName);

		bool push(string localSrc, string remoteDest)
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("pull");
			builder.AppendQuoted(localSrc);
			builder.AppendQuoted(remoteDest);

			return runner.RunAdb(Settings, builder);
		}

		public List<string> BugReport()
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("bugreport");

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);

			return output;
		}


		public List<string> Logcat(AdbLogcatOptions options = null)
		{
			// logcat[option][filter - specs]
			if (options == null)
				options = new AdbLogcatOptions();

			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("logcat");

			if (options.BufferType != AdbLogcatBufferType.Main)
			{
				builder.Append("-b");
				builder.Append(options.BufferType.ToString().ToLowerInvariant());
			}

			if (options.Clear || options.PrintSize)
			{
				if (options.Clear)
					builder.Append("-c");
				else if (options.PrintSize)
					builder.Append("-g");
			}
			else
			{
				// Always dump, since we want to return and not listen to logcat forever
				// in the future might be nice to add an alias that takes a cancellation token
				// and can pipe output until that token is cancelled.
				//if (options.Dump)
				builder.Append("-d");

				if (options.OutputFile != null)
				{
					builder.Append("-f");
					builder.AppendQuoted(options.OutputFile.FullName);

					if (options.NumRotatedLogs.HasValue)
					{
						builder.Append("-n");
						builder.Append(options.NumRotatedLogs.Value.ToString());
					}

					var kb = options.LogRotationKb ?? 16;
					builder.Append("-r");
					builder.Append(kb.ToString());
				}

				if (options.SilentFilter)
					builder.Append("-s");

				if (options.Verbosity != AdbLogcatOutputVerbosity.Brief)
				{
					builder.Append("-v");
					builder.Append(options.Verbosity.ToString().ToLowerInvariant());
				}

			}

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);

			return output;
		}

		public string Version()
		{
			// adb version
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("version");
			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);

			return string.Join(Environment.NewLine, output);
		}

		public string GetSerialNumber()
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("get-serialno");
			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);

			return string.Join(Environment.NewLine, output);
		}

		public string GetState()
		{
			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("get-state");
			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);

			return string.Join(Environment.NewLine, output);
		}


		public List<string> Shell(string shellCommand, AdbToolSettings settings = null)
		{
			if (settings == null)
				settings = new AdbToolSettings();

			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(settings.Serial, builder);

			builder.Append("shell");
			builder.Append(shellCommand);

			var output = new List<string>();
			runner.RunAdb(settings, builder, out output);

			return output;
		}


		public void ScreenCapture(FileInfo saveToLocalFile, AdbToolSettings settings = null)
		{
			if (settings == null)
				settings = new AdbToolSettings();

			//adb shell screencap / sdcard / screen.png
			var guid = Guid.NewGuid().ToString();
			var remoteFile = "/sdcard/" + guid + ".png";

			Shell("screencap " + remoteFile, settings);

			Pull(new FileInfo(remoteFile), saveToLocalFile);

			Shell("rm " + remoteFile, settings);
		}

		public void ScreenRecord(FileInfo saveToLocalFile, System.Threading.CancellationToken? recordingCancelToken = null, TimeSpan? timeLimit = null, int? bitrateMbps = null, int? width = null, int? height = null, bool rotate = false, bool logVerbose = false)
		{
			// screenrecord[options] filename

			var guid = Guid.NewGuid().ToString();
			var remoteFile = "/sdcard/" + guid + ".mp4";

			// adb uninstall -k <package>
			// -k keeps data & cache dir
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("screenrecord");

			if (timeLimit.HasValue)
			{
				builder.Append("--time-limit");
				builder.Append(((int)timeLimit.Value.TotalSeconds).ToString());
			}

			if (bitrateMbps.HasValue)
			{
				builder.Append("--bit-rate");
				builder.Append((bitrateMbps.Value * 1000000).ToString());
			}

			if (width.HasValue && height.HasValue)
			{
				builder.Append("--size");
				builder.Append($"{width}x{height}");
			}

			if (rotate)
				builder.Append("--rotate");

			if (logVerbose)
				builder.Append("--verbose");

			builder.Append(remoteFile);

			var output = new List<string>();

			if (recordingCancelToken.HasValue)
				runner.RunAdb(Settings, builder, recordingCancelToken.Value, out output);
			else
				runner.RunAdb(Settings, builder, out output);

			Pull(new FileInfo(remoteFile), saveToLocalFile);

			Shell("rm " + remoteFile);
		}
	}
}
