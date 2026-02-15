using System;
using System.Collections.Generic;
using System.IO;

namespace AndroidSdk
{
	public class Monkey : SdkTool
	{
		public Monkey()
			: this((DirectoryInfo)null, null)
		{ }

		public Monkey(DirectoryInfo androidSdkHome, string adbSerial)
			: base(androidSdkHome)
		{
			runner = new AdbRunner(this);
			AdbSerial = adbSerial;
		}

		public Monkey(DirectoryInfo androidSdkHome)
			: this(androidSdkHome, null)
		{
		}

		public Monkey(string androidSdkHome)
			: this(string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome), null)
		{
		}

		public Monkey(string androidSdkHome, string adbSerial)
			: this(string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome), adbSerial)
		{
		}

		public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
			=> FindTool(androidSdkHome, toolName: "adb", windowsExtension: ".exe", "platform-tools");

		public string AdbSerial { get; set; }

		readonly AdbRunner runner;

		public IEnumerable<string> LaunchDefaultLauncherActivity(string packageName)
		{
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("monkey");
			builder.Append("-p");
			builder.Append(packageName);
			builder.Append("-c");
			builder.Append("android.intent.category.LAUNCHER");
			builder.Append("1");

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}

		public IEnumerable<string> LaunchDefaultLauncherActivityVerbose(string packageName)
		{
			var builder = new ProcessArgumentBuilder();

			runner.AddSerial(AdbSerial, builder);

			builder.Append("shell");
			builder.Append("monkey");
			builder.Append("-p");
			builder.Append(packageName);
			builder.Append("-v");
			builder.Append("1");

			var r = runner.RunAdb(AndroidSdkHome, builder);
			return r.StandardOutput;
		}
	}
}
