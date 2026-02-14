using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidSdk.Tool
{
	public class AvdTeardownCommandSettings : CommandSettings
	{
		[Description("ADB serial of the emulator (e.g. emulator-5554)")]
		[CommandOption("-s|--serial")]
		public string Serial { get; set; }

		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("Directory to save collected logs and diagnostics")]
		[CommandOption("-o|--output|--output-directory")]
		[DefaultValue("artifacts/logs")]
		public string OutputDirectory { get; set; }

		[Description("Skip log collection and only shutdown")]
		[CommandOption("--skip-logs")]
		[DefaultValue(false)]
		public bool SkipLogs { get; set; }

		public override ValidationResult Validate()
		{
			if (string.IsNullOrEmpty(Serial))
				return ValidationResult.Error("Missing --serial");

			return ValidationResult.Success();
		}
	}

	public class AvdTeardownCommand : CancellableAsyncCommand<AvdTeardownCommandSettings>
	{
		public override Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] AvdTeardownCommandSettings settings, CancellationToken cancellationToken)
		{
			var adb = new Adb(settings?.Home);
			var outputDir = settings.OutputDirectory;
			Directory.CreateDirectory(outputDir);

			if (!settings.SkipLogs)
			{
				// Collect logcat
				try
				{
					AnsiConsole.MarkupLine("[dim]Collecting logcat...[/]");
					var logcat = adb.Logcat(adbSerial: settings.Serial);
					var logcatPath = Path.Combine(outputDir, "logcat.txt");
					File.WriteAllLines(logcatPath, logcat);
					AnsiConsole.MarkupLine($"[green]Logcat saved to {logcatPath}[/]");
				}
				catch (Exception ex)
				{
					AnsiConsole.MarkupLine($"[yellow]Failed to collect logcat: {ex.Message}[/]");
				}

				// Collect bugreport
				try
				{
					AnsiConsole.MarkupLine("[dim]Collecting bugreport...[/]");
					var bugreport = adb.BugReport(settings.Serial);
					var bugreportPath = Path.Combine(outputDir, "bugreport.txt");
					File.WriteAllLines(bugreportPath, bugreport);
					AnsiConsole.MarkupLine($"[green]Bugreport saved to {bugreportPath}[/]");
				}
				catch (Exception ex)
				{
					AnsiConsole.MarkupLine($"[yellow]Failed to collect bugreport: {ex.Message}[/]");
				}

				// Pull ANR traces
				try
				{
					var anrDir = new DirectoryInfo(Path.Combine(outputDir, "anr"));
					anrDir.Create();
					adb.Pull(new DirectoryInfo("/data/anr"), anrDir, settings.Serial);
					if (anrDir.GetFiles().Length > 0)
						AnsiConsole.MarkupLine($"[green]ANR traces saved to {anrDir.FullName}[/]");
				}
				catch { }

				// Pull tombstones
				try
				{
					var tombDir = new DirectoryInfo(Path.Combine(outputDir, "tombstones"));
					tombDir.Create();
					adb.Pull(new DirectoryInfo("/data/tombstones"), tombDir, settings.Serial);
					if (tombDir.GetFiles().Length > 0)
						AnsiConsole.MarkupLine($"[green]Tombstones saved to {tombDir.FullName}[/]");
				}
				catch { }
			}

			// Shutdown emulator
			try
			{
				AnsiConsole.MarkupLine("[dim]Shutting down emulator...[/]");
				adb.EmuKill(settings.Serial);
				AnsiConsole.MarkupLine("[green]Emulator shutdown complete[/]");
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine($"[yellow]Failed to gracefully shutdown: {ex.Message}[/]");
			}

			return Task.FromResult(0);
		}
	}
}
