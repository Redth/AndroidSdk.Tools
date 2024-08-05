#nullable enable
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace AndroidSdk.Tool;

public class DeviceCommandSettings : BaseDeviceCommandSettings
{
	[Description("Serial or Device to filter for")]
	[CommandOption("-d|--id|--device|--serial")]
	public string[]? Devices { get; set; }
}

public abstract class DeviceCommand<TSettings> : BaseDeviceCommand<TSettings>
	where TSettings : DeviceCommandSettings
{
	protected static bool IsDeviceMatch(string value, string pattern) =>
		pattern.Equals(value, StringComparison.InvariantCultureIgnoreCase) ||
		Regex.IsMatch(value, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

	public abstract int Execute([NotNull] CommandContext context, [NotNull] TSettings settings, [NotNull] Adb adb, [NotNull] IEnumerable<Adb.AdbDevice> devices);

	public override int Execute([NotNull] CommandContext context, [NotNull] TSettings settings, [NotNull] Adb adb)
	{
		var devices = adb.GetDevices();

		var hasFilter = settings.Devices?.Any() == true;

		var filtered = hasFilter
			? devices.Where(d => settings.Devices!.Any(s => IsDeviceMatch(d.Serial, s))).ToArray()
			: devices.ToArray();

		return Execute(context, settings, adb, filtered);
	}
}
