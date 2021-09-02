using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool
{
	public class DevicesListCommandSettings : CommandSettings
	{
		[Description("Output Format")]
		[CommandOption("-f|--format")]
		[DefaultValue(OutputFormat.None)]
		[TypeConverter(typeof(OutputFormatTypeConverter))]
		public OutputFormat Format { get; set; }

		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }
	}

	public class DevicesListCommand : Command<DevicesListCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] DevicesListCommandSettings settings)
		{
			try
			{
				var adb = new Adb(settings?.Home);
				var devices = adb.GetDevices();

				if ((settings?.Format ?? OutputFormat.None) == OutputFormat.None)
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

			}
			catch (SdkToolFailedExitException sdkEx)
			{
				Program.WriteException(sdkEx);
				return 1;
			}

			return 0;
		}
	}
}
