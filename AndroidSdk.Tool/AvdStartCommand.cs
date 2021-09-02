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
	public class AvdStartCommandSettings : CommandSettings
	{
		[Description("Name of the AVD/Emulator")]
		[CommandOption("-n|--name")]
		public string Name { get; set; }

		[Description("Wait for emulator to boot")]
		[CommandOption("-w|--wait|--wait-boot")]
		[DefaultValue(false)]
		public bool WaitForBoot { get; set; }

		[Description("Wait for emulator process to exit")]
		[CommandOption("--wait-exit")]
		[DefaultValue(false)]
		public bool WaitForExit { get; set; }

		[Description("Timeout in seconds if waiting for emulator to boot")]
		[CommandOption("-t|--timeout")]
		public int? Timeout { get; set; }

		[Description("Wipe data")]
		[CommandOption("--wipe|--wipe-data")]
		[DefaultValue(false)]
		public bool WipeData { get; set; }

		[Description("Android SDK home/root path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("Disable Snapshot load/save")]
		[CommandOption("--no-snapshot")]
		[DefaultValue(false)]
		public bool NoSnapshot { get; set; }

		public override ValidationResult Validate()
		{
			if (string.IsNullOrEmpty(Name))
				return ValidationResult.Error("Missing --name");

			return ValidationResult.Success();
		}
	}

	public class AvdStartCommand : Command<AvdStartCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdStartCommandSettings settings)
		{
			var ok = true;

			try
			{
				var emu = new Emulator(settings?.Home);

				Emulator.AndroidEmulatorProcess process = null;

				AnsiConsole.Status()
				.Start($"Starting {settings.Name}...", ctx =>
				{
					process = emu.Start(settings.Name, new Emulator.EmulatorStartOptions
					{
						WipeData = settings.WipeData,
						NoSnapshot = settings.NoSnapshot
					});

					var timeout = settings.Timeout.HasValue ? TimeSpan.FromSeconds(settings.Timeout.Value) : TimeSpan.Zero;

					if (settings.WaitForBoot)
					{
						ctx.Status($"Waiting for {settings.Name} to finish booting...");
						ok = process.WaitForBootComplete(timeout);
					}

					if (settings.WaitForExit)
					{
						ctx.Status($"Booted, waiting for {settings.Name} to exit...");
						ok = process.WaitForExit() == 0;
					}
				});

				if (!ok)
				{
					AnsiConsole.WriteException(new Exception("Failed to start AVD: " + string.Join(Environment.NewLine, process.GetStandardOutput())));
				}

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
