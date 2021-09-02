using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidSdk.Tool
{
	public class AvdDevicesCommandSettings : CommandSettings
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

	public class AvdDevicesCommand : Command<AvdDevicesCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdDevicesCommandSettings settings)
		{
			try
			{
				var avd = new AvdManager(settings?.Home);

				var devices = avd.ListDevices();

				OutputHelper.Output(devices, settings?.Format,
					new[] { "Name", "Id", "NumericId", "Oem" },
					i => new[] { i.Name, i.Id, i.NumericId?.ToString() ?? string.Empty, i.Oem });
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
