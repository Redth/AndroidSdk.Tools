using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool
{
	public class AvdTargetsCommandSettings : CommandSettings
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

	public class AvdTargetsCommand : Command<AvdTargetsCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdTargetsCommandSettings settings)
		{
			try
			{
				var avd = new AvdManager(settings?.Home);

				var targets = avd.ListTargets();

				OutputHelper.Output(targets, settings?.Format,
					new[] { "Name", "Id", "Numeric Id", "API Level", "Type", "Revision" },
					i => new[] { i.Name, i.Id, i.NumericId?.ToString() ?? string.Empty, i.ApiLevel.ToString(), i.Type, i.Revision.ToString() });
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
