using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool
{
	public class AvdListCommandSettings : CommandSettings
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

	public class AvdListCommand : Command<AvdListCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdListCommandSettings settings)
		{
			try
			{
				var avd = new AvdManager(settings?.Home);

				var avds = avd.ListAvds();

				OutputHelper.Output(avds, settings?.Format,
					new[] { "Name", "Target", "Device", "Based On", "Path" },
					i => new[] { i.Name, i.Target, i.Device, i.BasedOn, i.Path });
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
