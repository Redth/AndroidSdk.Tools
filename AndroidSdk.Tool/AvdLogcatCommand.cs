using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace AndroidSdk.Tool
{
	public class AvdLogcatCommandSettings : CommandSettings
	{
		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("ADB serial of the emulator (optional - uses first device if not specified)")]
		[CommandOption("-s|--serial")]
		public string Serial { get; set; }

		[Description("Output file path (if not specified, dumps to stdout)")]
		[CommandOption("-o|--output")]
		public string OutputPath { get; set; }
	}

	public class AvdLogcatCommand : Command<AvdLogcatCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdLogcatCommandSettings settings)
		{
			try
			{
				var adb = new Adb(settings?.Home);

				var options = new Adb.AdbLogcatOptions();

				var lines = adb.Logcat(options, adbSerial: settings.Serial, allowNonZeroExitWithStdOut: true);

				if (!string.IsNullOrEmpty(settings.OutputPath))
				{
					var dir = Path.GetDirectoryName(settings.OutputPath);
					if (!string.IsNullOrEmpty(dir))
						Directory.CreateDirectory(dir);

					File.WriteAllLines(settings.OutputPath, lines);
					Console.Error.WriteLine($"Logcat written to {settings.OutputPath} ({lines.Count} lines)");
				}
				else
				{
					foreach (var line in lines)
						Console.WriteLine(line);
				}
			}
			catch (SdkToolFailedExitException sdkEx)
			{
				Program.WriteException(sdkEx);
				return 1;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error: {ex.Message}");
				return 1;
			}
			return 0;
		}
	}
}
