using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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
		public DirectoryInfo? Home { get; set; }

		[Description("Java JDK Home Path")]
		[CommandOption("-j|--jdk")]
		public DirectoryInfo? JdkHome { get; set; }
	}

	public class AvdTargetsCommand : Command<AvdTargetsCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdTargetsCommandSettings settings)
		{
			try
			{
				var sdk = new AndroidSdkManager(settings.Home, settings.JdkHome);

				var targets = sdk.AvdManager.ListTargets();

				OutputHelper.Output(targets, settings?.Format,
					[ "Name", "Id", "Numeric Id", "API Level", "Type", "Revision" ],
					i => [ i.Name, i.Id, i.NumericId?.ToString() ?? string.Empty, i.ApiLevel?.ToString() ?? string.Empty, i.Type, i.Revision?.ToString() ?? string.Empty ]);
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
