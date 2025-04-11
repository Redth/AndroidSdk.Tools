using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace AndroidSdk.Tool
{
	public class SdkListCommandSettings : CommandSettings
	{
		[Description("Show available packages to install")]
		[CommandOption("--available")]
		public bool Available { get; set; }

		[Description("Show all packages")]
		[CommandOption("--all")]
		public bool All { get; set; }

		[Description("Show available packages to install")]
		[CommandOption("--installed")]
		public bool Installed { get; set; }

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

	public class SdkListCommand : Command<SdkListCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkListCommandSettings settings)
		{
			try
			{
				var sdk = new AndroidSdkManager(settings.Home, settings.JdkHome);

				var sdkList = sdk.SdkManager.List();

				if (settings.Available || settings.Installed)
				{
					if (!settings.Available)
						sdkList.AvailablePackages.Clear();
					if (!settings.Installed)
						sdkList.InstalledPackages.Clear();
				}

				if (settings.Format == OutputFormat.None)
				{
					if (sdkList.AvailablePackages.Any())
					{
						var rule = new Rule("Available Packages:");
						rule.Centered();
						AnsiConsole.Write(rule);

						OutputHelper.OutputTable(sdkList.AvailablePackages,
							[ "Package", "Version", "Description" ],
							i => [ i.Path, i.Version, i.Description ?? string.Empty ]);
					}

					if (sdkList.InstalledPackages.Any())
					{
						var rule = new Rule("Installed Packages:");
						rule.Centered();
						AnsiConsole.Write(rule);

						OutputHelper.OutputTable(
							sdkList.InstalledPackages,
							[ "Package", "Version", "Description", "Location" ],
							i => [ i.Path, i.Version, i.Description ?? string.Empty, i.Location ]);
					}
				}
				else
				{
					OutputHelper.Output(sdkList, settings.Format);
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
