using Spectre.Console;
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
	public class AvdDeleteCommandSettings : CommandSettings
	{
		[Description("Name of the AVD/Emulator")]
		[CommandOption("-n|--name")]
		public string Name { get; set; }

		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		public override ValidationResult Validate()
		{
			if (string.IsNullOrEmpty(Name))
				return ValidationResult.Error("Missing --name");

			return ValidationResult.Success();
		}
	}

	public class AvdDeleteCommand : Command<AvdDeleteCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdDeleteCommandSettings settings)
		{
			try
			{
				var avd = new AvdManager(settings?.Home);

				avd.Delete(settings.Name);
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
