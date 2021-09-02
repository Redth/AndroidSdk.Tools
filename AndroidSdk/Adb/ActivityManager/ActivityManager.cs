using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndroidSdk
{
	public partial class ActivityManager : SdkTool
	{
		public ActivityManager()
			: this((DirectoryInfo)null, null)
		{ }

		public ActivityManager(DirectoryInfo androidSdkHome, string adbSerial)
			: base(androidSdkHome)
		{
			runner = new AdbRunner(this);
			AdbSerial = adbSerial;
		}

		public ActivityManager(DirectoryInfo androidSdkHome)
			: this(androidSdkHome, null)
		{
		}

		public ActivityManager(string androidSdkHome)
			: this(string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome), null)
		{
		}

		public ActivityManager(string androidSdkHome, string adbSerial)
			: this(string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome), adbSerial)
		{
		}

		internal override string SdkPackageId => "platform-tools";

		public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
			=> FindTool(androidSdkHome, toolName: "adb", windowsExtension: ".exe", "platform-tools");

		public string AdbSerial { get; set; }

		readonly AdbRunner runner;

		public bool StartActivity(string adbIntentArguments, ActivityManagerStartOptions options = null)
		{
			if (options == null)
				options = new ActivityManagerStartOptions();

			// start [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

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

			var r = runner.RunAdb(AndroidSdkHome, builder);

			return r.StandardOutput.Any(l => l.StartsWith("Starting:", StringComparison.OrdinalIgnoreCase));
		}

		public bool StartService(string adbIntentArguments, string runAsUser = null)
		{
			// startservice [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("startservice");

			if (!string.IsNullOrEmpty(runAsUser))
			{
				builder.Append("--user");
				builder.Append(runAsUser);
			}

			builder.Append(adbIntentArguments);

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput.Any(l => l.StartsWith("Starting service:", StringComparison.OrdinalIgnoreCase));
		}

		public void ForceStop(string packageName)
		{
			//force-stop package
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("force-stop");
			builder.Append(packageName);

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public void Kill(string packageName, string forUser = null)
		{
			// kill[options] package
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("kill");
			builder.Append(packageName);

			if (!string.IsNullOrEmpty(forUser))
			{
				builder.Append("--user");
				builder.Append(forUser);
			}

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public void KillAll()
		{
			// killall
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("killall");

			runner.RunAdb(AndroidSdkHome, builder);
		}

		public int Broadcast(string intent, string toUser = null)
		{
			const string rxBroadcastResult = "Broadcast completed:\\s+result\\s?=\\s?(?<result>[0-9]+)";

			// broadcast [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("broadcast");

			if (!string.IsNullOrEmpty(toUser))
			{
				builder.Append("--user");
				builder.Append(toUser);
			}

			builder.Append(intent);

			var r = runner.RunAdb(AndroidSdkHome, builder);

			foreach (var line in r.StandardOutput)
			{
				var match = Regex.Match(line, rxBroadcastResult, RegexOptions.Singleline | RegexOptions.IgnoreCase);
				var m = match?.Groups?["result"]?.Value ?? "-1";

				if (int.TryParse(m, out var rInt))
					return rInt;
			}

			return -1;
		}

		public List<string> Instrument(string component, ActivityManagerInstrumentOptions options = null)
		{
			// instrument [options] component
			if (options == null)
				options = new ActivityManagerInstrumentOptions();

			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

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

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public List<string> StartProfiling(string process, FileInfo outputFile)
		{
			// broadcast [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("profile");
			builder.Append("start");

			builder.Append(process);

			builder.AppendQuoted(outputFile.FullName);

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public List<string> StopProfiling(string process)
		{
			// broadcast [options] intent
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("profile");
			builder.Append("stop");

			builder.Append(process);

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public List<string> DumpHeap(string process, FileInfo outputFile, string forUser = null, bool dumpNativeHeap = false)
		{
			// dumpheap [options] process file
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

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

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public List<string> SetDebugApp(string packageName, bool wait = false, bool persistent = false)
		{
			// set-debug-app [options] package
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("set-debug-app");

			if (wait)
				builder.Append("-w");

			if (persistent)
				builder.Append("--persistent");

			builder.Append(packageName);

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public List<string> ClearDebugApp()
		{
			// clear-debug-app
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("clear-debug-app");

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public List<string> Monitor(int? gdbPort = null)
		{
			// monitor [options]
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("monitor");

			if (gdbPort.HasValue)
				builder.Append("--gdb:" + gdbPort.Value);

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public List<string> ScreenCompat(bool compatOn, string packageName)
		{
			// screen-compat {on|off} package
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("screen-compat");

			builder.Append(compatOn ? "on" : "off");

			builder.Append(packageName);

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
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

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("display-size");

			if (reset)
				builder.Append("reset");
			else
				builder.Append(string.Format("{0}x{1}", width, height));

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public List<string> DisplayDensity(int dpi)
		{
			// display-density dpi
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("display-density");

			builder.Append(dpi.ToString());

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public string IntentToURI(string intent)
		{
			// display-density dpi
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("to-uri");

			builder.Append(intent);

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return string.Join(Environment.NewLine, r.StandardOutput);
		}

		public string IntentToIntentURI(string intent)
		{
			// display-density dpi
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("am");

			builder.Append("to-intent-uri");

			builder.Append(intent);

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return string.Join(Environment.NewLine, r.StandardOutput);
		}
	}
}
