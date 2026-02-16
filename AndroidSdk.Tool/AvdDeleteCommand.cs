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

		[Description("Stop the running emulator before deleting (never fails)")]
		[CommandOption("--force")]
		[DefaultValue(false)]
		public bool Force { get; set; }

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
				if (settings.Force)
				{
					var emu = new Emulator(settings?.Home);
					var stopped = emu.StopAvd(settings.Name, TimeSpan.FromSeconds(10));
					if (stopped)
					{
						AnsiConsole.MarkupLine($"[yellow]Stopped running emulator for AVD '{settings.Name}'.[/]");
					}
				}

				var avd = new AvdManager(settings?.Home);

				avd.Delete(settings.Name);
			}
			catch (SdkToolFailedExitException sdkEx)
			{
				if (settings.Force)
				{
					// --force never fails (cleanup command)
					AnsiConsole.MarkupLine($"[yellow]AVD '{settings.Name}' may not exist (ignored with --force)[/]");
					return 0;
				}
				Program.WriteException(sdkEx);
				return 1;
			}
			catch (Exception)
			{
				if (settings.Force)
				{
					AnsiConsole.MarkupLine($"[yellow]Warning: failed to force-delete AVD '{settings.Name}' due to unexpected error.[/]");
					return 0;
				}
				throw;
			}
			return 0;
		}
	}
}
