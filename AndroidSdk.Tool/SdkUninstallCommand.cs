using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool
{
	public class SdkUninstallCommandSettings : CommandSettings
	{
		[Description("Package to uninstall")]
		[CommandOption("-p|--package")]
		public string[] Package { get; set; }

		[Description("Output Format")]
		[CommandOption("-f|--format")]
		[DefaultValue(OutputFormat.None)]
		[TypeConverter(typeof(OutputFormatTypeConverter))]
		public OutputFormat Format { get; set; }

		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }
	}

	public class SdkUninstallCommand : Command<SdkUninstallCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkUninstallCommandSettings settings)
		{
			var ok = true;

			try
			{
				var m = new SdkManager(settings?.Home);

				ok = m.Uninstall(settings.Package);

			}
			catch (SdkToolFailedExitException sdkEx)
			{
				Program.WriteException(sdkEx);
				return 1;
			}

			return ok ? 0 : 1;
		}
	}
}
