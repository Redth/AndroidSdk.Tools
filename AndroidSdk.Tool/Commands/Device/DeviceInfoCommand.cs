#nullable enable
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace AndroidSdk.Tool;

public class DeviceInfoCommandSettings : DeviceCommandSettings
{
	[Description("Property to include in output")]
	[CommandOption("-p|--prop|--property")]
	public string[]? Properties { get; set; }
}

public class DeviceInfoCommand : DeviceCommand<DeviceInfoCommandSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] DeviceInfoCommandSettings settings, [NotNull] Adb adb, [NotNull] IEnumerable<Adb.AdbDevice> devices)
	{
		var results = new List<DeviceWrapper>();

		foreach (var device in devices)
		{
			var props = adb.GetProperties(device.Serial, settings.Properties);

			if (settings.Format == OutputFormat.None)
			{
				var rule = new Rule(device.Serial);
				AnsiConsole.Write(rule);

				OutputHelper.OutputTable(
					props,
					new[] { "Property Name", "Property Value" },
					i => new[] { i.Key, i.Value });

				AnsiConsole.WriteLine();
			}
			results.Add(new DeviceWrapper
			{
				Device = device,
				Properties = adb.GetProperties(device.Serial, settings.Properties)
			});
		}

		if (settings.Format != OutputFormat.None)
		{
			OutputHelper.Output(results, settings.Format);
		}

		return 0;
	}

	[DataContract]
	internal class DeviceWrapper
	{
		[DataMember(Name = "device")]
		public Adb.AdbDevice Device { get; set; } = null!;

		[DataMember(Name = "properties")]
		public Dictionary<string, string> Properties { get; set; } = new();
	}
}
