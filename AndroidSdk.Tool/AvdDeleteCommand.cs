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
					// Find and stop the running emulator by AVD name
					try
					{
						var adb = new Adb(settings?.Home);
						var devices = adb.GetDevices();
						foreach (var d in devices)
						{
							try
							{
								var name = adb.GetEmulatorName(d.Serial);
								if (name.Equals(settings.Name, StringComparison.OrdinalIgnoreCase))
								{
									AnsiConsole.MarkupLine($"[yellow]Stopping emulator {d.Serial} ({settings.Name})...[/]");
									adb.EmuKill(d.Serial);
									// Wait briefly for process to terminate
									System.Threading.Thread.Sleep(3000);
									break;
								}
							}
							catch { }
						}
					}
					catch { }
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
					return 0;
				throw;
			}
			return 0;
		}
	}
}
