using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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
		public DirectoryInfo? Home { get; set; }

		[Description("Java JDK Home Path")]
		[CommandOption("-j|--jdk")]
		public DirectoryInfo? JdkHome { get; set; }
	}

	public class AvdListCommand : Command<AvdListCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdListCommandSettings settings)
		{
			try
			{
				var sdk = new AndroidSdkManager(settings.Home, settings.JdkHome);

				var avds = sdk.AvdManager.ListAvds();

				OutputHelper.Output(avds, settings?.Format,
					[ "Name", "Target", "Device", "Based On", "Path" ],
					i => [ i.Name, i.Target, i.Device, i.BasedOn ?? string.Empty, i.Path ]);
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
