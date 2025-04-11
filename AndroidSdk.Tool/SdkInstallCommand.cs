using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace AndroidSdk.Tool
{
	public class SdkInstallCommandSettings : CommandSettings
	{
		[Description("Package to install")]
		[CommandOption("-p|--package")]
		public string[] Package { get; set; } = [];

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

		public override ValidationResult Validate()
		{
			if (Package is null || Package.Length <= 0)
				return ValidationResult.Error("One or more --package argumentss are required");

			return base.Validate();
		}
	}

	public class SdkInstallCommand : Command<SdkInstallCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkInstallCommandSettings settings)
		{
			var ok = true;
			try
			{
				var sdk = new AndroidSdkManager(settings.Home, settings.JdkHome);

				ok = sdk.SdkManager.Install(settings.Package);
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
