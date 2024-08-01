#nullable enable
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool;

public class DevicesListCommandSettings : BaseDeviceCommandSettings
{
}

public class DevicesListCommand : BaseDeviceCommand<DevicesListCommandSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] DevicesListCommandSettings settings, [NotNull] Adb adb)
	{
		var devices = adb.GetDevices();

		if (settings.Format == OutputFormat.None)
		{
			OutputHelper.OutputTable(
				devices,
				new[] { "Serial", "Emulator", "Device", "Model", "Product" },
				i => new[] { i.Serial, i.IsEmulator.ToString(), i.Device, i.Model, i.Product });
		}
		else
		{
			OutputHelper.Output(devices, settings.Format);
		}

		return 0;
	}
}
