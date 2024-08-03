#nullable enable
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AndroidSdk.Tool;

public abstract class SingleDeviceCommand<TSettings> : DeviceCommand<TSettings>
	where TSettings : DeviceCommandSettings
{
	public abstract int Execute([NotNull] CommandContext context, [NotNull] TSettings settings, [NotNull] Adb adb, [NotNull] Adb.AdbDevice device);

	public override int Execute([NotNull] CommandContext context, [NotNull] TSettings settings, [NotNull] Adb adb, [NotNull] IEnumerable<Adb.AdbDevice> devices)
	{
		var array = devices.ToArray();

		if (array.Length == 0)
		{
			if (settings.Devices?.Any() == true)
				throw new InvalidOperationException("No device was found matching the filter.");
			else
				throw new InvalidOperationException("No devices was found, make sure to specify a single device using --device.");
		}
		else if (array.Length != 1)
		{
			throw new InvalidOperationException("More than one device was found, please specify a more specific device.");
		}

		return Execute(context, settings, adb, array[0]);
	}
}
