using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool
{
	public class SdkInstallCommandSettings : CommandSettings
	{
		[Description("Package to install")]
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

	public class SdkInstallCommand : Command<SdkInstallCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkInstallCommandSettings settings)
		{
			var ok = true;
			try
			{
				var m = new SdkManager(settings?.Home);
				m.OutputHandler = line => Console.WriteLine(line);
				m.ErrorHandler = line => Console.Error.WriteLine(line);

				ok = m.Install(settings.Package);
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
