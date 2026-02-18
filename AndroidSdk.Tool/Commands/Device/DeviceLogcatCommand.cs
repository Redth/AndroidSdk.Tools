#nullable enable
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace AndroidSdk.Tool;

public class DeviceLogcatCommandSettings : DeviceCommandSettings
{
	[Description("Output file path (if not specified, dumps to stdout)")]
	[CommandOption("-o|--output")]
	public string? OutputPath { get; set; }
}

public class DeviceLogcatCommand : SingleDeviceCommand<DeviceLogcatCommandSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] DeviceLogcatCommandSettings settings, [NotNull] Adb adb, [NotNull] Adb.AdbDevice device)
	{
		var options = new Adb.AdbLogcatOptions();
		var lines = adb.Logcat(options, adbSerial: device.Serial);

		if (!string.IsNullOrEmpty(settings.OutputPath))
		{
			var dir = Path.GetDirectoryName(settings.OutputPath);
			if (!string.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);

			File.WriteAllLines(settings.OutputPath, lines);
			System.Console.Error.WriteLine($"Logcat written to {settings.OutputPath} ({lines.Count} lines)");
		}
		else
		{
			foreach (var line in lines)
				System.Console.WriteLine(line);
		}

		return 0;
	}
}
