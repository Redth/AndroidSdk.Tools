using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AndroidSdk.Tool
{
	public class DeviceInfoCommandSettings : CommandSettings
	{
		[Description("Output Format")]
		[CommandOption("-f|--format")]
		[DefaultValue(OutputFormat.None)]
		[TypeConverter(typeof(OutputFormatTypeConverter))]
		public OutputFormat Format { get; set; }

		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("Property to include in output")]
		[CommandOption("-p|--prop|--property")]
		public string[] Properties { get; set; }

		[Description("Serial or Device to filter for")]
		[CommandOption("-d|--id|--device|--serial")]
		public string[] Devices { get; set; }
	}

	public class DeviceInfoCommand : Command<DeviceInfoCommandSettings>
	{
		static bool IsPropertyMatch(string value, string pattern)
		{
			if (pattern.Equals(value, StringComparison.InvariantCultureIgnoreCase)
				|| Regex.IsMatch(value, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
				return true;

			return false;
		}

		public override int Execute([NotNull] CommandContext context, [NotNull] DeviceInfoCommandSettings settings)
		{
			try
			{
				var adb = new Adb(settings?.Home);
				var devices = adb.GetDevices();

				var deviceFilterSpecified = settings.Devices?.Any() ?? false;

				var results = new List<DeviceWrapper>();

				foreach (var device in devices)
				{
					// If filtering on device, check that this is in the list
					if (deviceFilterSpecified && !settings.Devices.Any(d => IsPropertyMatch(device.Serial, d)))
						continue;

					var props = adb.GetProperties(device.Serial, settings.Properties);

					if (settings.Format == OutputFormat.None)
					{
						var rule = new Rule(device.Serial);
						AnsiConsole.Render(rule);

						OutputHelper.OutputTable(props, new[] { "Property Name", "Property Value" },
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

			}
			catch (SdkToolFailedExitException sdkEx)
			{
				Program.WriteException(sdkEx);
				return 1;
			}

			return 0;
		}

		[DataContract]
		internal class DeviceWrapper
		{
			[DataMember(Name = "device")]
			public Adb.AdbDevice Device { get; set; }

			[DataMember(Name = "properties")]
			public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
		}
	}
}
