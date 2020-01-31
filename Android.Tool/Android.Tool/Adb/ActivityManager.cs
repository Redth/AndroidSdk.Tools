using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Android.Tool.Adb
{
	public class ActivityManager
	{
		public ActivityManager(AdbToolSettings settings)
		{
			runner = new AdbToolRunner();
			Settings = settings;
		}

		public ActivityManager(DirectoryInfo androidSdkHome)
			: this(new AdbToolSettings { AndroidSdkRoot = androidSdkHome })
		{
		}

		public ActivityManager(DirectoryInfo androidSdkHome, string deviceSerial)
			: this(new AdbToolSettings { AndroidSdkRoot = androidSdkHome, Serial = deviceSerial })
		{
		}


		public ActivityManager(string androidSdkHome)
		: this(new AdbToolSettings { AndroidSdkRoot = new DirectoryInfo(androidSdkHome) })
		{
		}

		public ActivityManager(string androidSdkHome, string deviceSerial)
		: this(new AdbToolSettings { AndroidSdkRoot = new DirectoryInfo(androidSdkHome), Serial = deviceSerial })
		{
		}

		public AdbToolSettings Settings { get; set; }
		readonly AdbToolRunner runner;

		public bool StartActivity(string adbIntentArguments, AmStartOptions options = null)
		{
			if (options == null)
				options = new AmStartOptions();

			// start [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("start");

			if (options.EnableDebugging)
				builder.Append("-D");
			if (options.WaitForLaunch)
				builder.Append("-W");
			if (options.ProfileToFile != null)
			{
				if (options.ProfileUntilIdle)
					builder.Append("-P");
				else
					builder.Append("--start");
				builder.AppendQuoted(options.ProfileToFile.FullName);
			}
			if (options.RepeatLaunch.HasValue && options.RepeatLaunch.Value > 0)
			{
				builder.Append("-R");
				builder.Append(options.RepeatLaunch.Value.ToString());
			}
			if (options.ForceStopTarget)
				builder.Append("-S");
			if (options.EnableOpenGLTrace)
				builder.Append("--opengl-trace");
			if (!string.IsNullOrEmpty(options.RunAsUserId))
			{
				builder.Append("--user");
				builder.Append(options.RunAsUserId);
			}

			builder.Append(adbIntentArguments);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);

			return output.Any(l => l.StartsWith("Starting:", StringComparison.OrdinalIgnoreCase));
		}

		public bool StartService(string adbIntentArguments, string runAsUser = null)
		{
			// startservice [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("startservice");

			if (!string.IsNullOrEmpty(runAsUser))
			{
				builder.Append("--user");
				builder.Append(runAsUser);
			}

			builder.Append(adbIntentArguments);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output.Any(l => l.StartsWith("Starting service:", StringComparison.OrdinalIgnoreCase));
		}

		public void ForceStop(string packageName)
		{
			//force-stop package
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("force-stop");
			builder.Append(packageName);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
		}

		public void Kill(string packageName, string forUser = null)
		{
			// kill[options] package
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("kill");
			builder.Append(packageName);

			if (!string.IsNullOrEmpty(forUser))
			{
				builder.Append("--user");
				builder.Append(forUser);
			}

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
		}

		public void KillAll()
		{
			// killall
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("killall");

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
		}

		public int Broadcast(string intent, string toUser = null)
		{
			const string rxBroadcastResult = "Broadcast completed:\\s+result\\s?=\\s?(?<result>[0-9]+)";

			// broadcast [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("broadcast");

			if (!string.IsNullOrEmpty(toUser))
			{
				builder.Append("--user");
				builder.Append(toUser);
			}

			builder.Append(intent);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);

			foreach (var line in output)
			{
				var match = Regex.Match(line, rxBroadcastResult, RegexOptions.Singleline | RegexOptions.IgnoreCase);
				var r = match?.Groups?["result"]?.Value ?? "-1";
				var rInt = -1;
				if (int.TryParse(r, out rInt))
					return rInt;
			}

			return -1;
		}

		public List<string> Instrument(string component, AmInstrumentOptions options = null)
		{
			// instrument [options] component
			if (options == null)
				options = new AmInstrumentOptions();

			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("instrument");

			if (options.PrintRawResults)
				builder.Append("-r");

			foreach (var kvp in options.KeyValues)
			{
				var v = string.Join(",", kvp.Value);
				builder.Append($"-e {kvp.Key} {v}");
			}

			if (options.ProfileToFile != null)
			{
				builder.Append("-p");
				builder.AppendQuoted(options.ProfileToFile.FullName);
			}

			if (options.Wait)
				builder.Append("-w");

			if (options.NoWindowAnimation)
				builder.Append("--no-window-animation");


			if (!string.IsNullOrEmpty(options.RunAsUser))
			{
				builder.Append("--user");
				builder.Append(options.RunAsUser);
			}

			builder.Append(component);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public List<string> StartProfiling(string process, FileInfo outputFile)
		{
			// broadcast [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("profile");
			builder.Append("start");

			builder.Append(process);

			builder.AppendQuoted(outputFile.FullName);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public List<string> StopProfiling(string process)
		{
			// broadcast [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("profile");
			builder.Append("stop");

			builder.Append(process);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public List<string> DumpHeap(string process, FileInfo outputFile, string forUser = null, bool dumpNativeHeap = false)
		{
			// dumpheap [options] process file
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("dumpheap");

			if (!string.IsNullOrEmpty(forUser))
			{
				builder.Append("--user");
				builder.Append(forUser);
			}

			if (dumpNativeHeap)
				builder.Append("-n");

			builder.Append(process);

			builder.AppendQuoted(outputFile.FullName);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public List<string> SetDebugApp(string packageName, bool wait = false, bool persistent = false)
		{
			// set-debug-app [options] package
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("set-debug-app");

			if (wait)
				builder.Append("-w");

			if (persistent)
				builder.Append("--persistent");

			builder.Append(packageName);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public List<string> ClearDebugApp()
		{
			// clear-debug-app
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("clear-debug-app");

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public List<string> Monitor(int? gdbPort = null)
		{
			// monitor [options]
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("monitor");

			if (gdbPort.HasValue)
				builder.Append("--gdb:" + gdbPort.Value);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public List<string> ScreenCompat(bool compatOn, string packageName)
		{
			// screen-compat {on|off} package
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("screen-compat");

			builder.Append(compatOn ? "on" : "off");

			builder.Append(packageName);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public List<string> DisplaySize(int width, int height)
		{
			return displaySize(false, width, height);
		}
		public List<string> ResetDisplaySize()
		{
			return displaySize(true, -1, -1);
		}
		List<string> displaySize(bool reset, int width, int height)
		{
			// display-size [reset|widthxheight]
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("display-size");

			if (reset)
				builder.Append("reset");
			else
				builder.Append(string.Format("{0}x{1}", width, height));

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public List<string> DisplayDensity(int dpi)
		{
			// display-density dpi
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("display-density");

			builder.Append(dpi.ToString());

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return output;
		}

		public string IntentToURI(string intent)
		{
			// display-density dpi
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("to-uri");

			builder.Append(intent);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return string.Join(Environment.NewLine, output);
		}

		public string IntentToIntentURI(string intent)
		{
			// display-density dpi
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(Settings.Serial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("to-intent-uri");

			builder.Append(intent);

			var output = new List<string>();
			runner.RunAdb(Settings, builder, out output);
			return string.Join(Environment.NewLine, output);
		}
	}
}
