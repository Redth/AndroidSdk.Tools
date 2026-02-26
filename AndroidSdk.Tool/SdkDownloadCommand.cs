using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidSdk.Tool
{
	public class SdkDownloadCommandSettings : CommandSettings
	{
		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("Forces re-download of cmdline-tools even if already installed")]
		[CommandOption("-f|--force")]
		public bool Force { get; set; }

		[Description("Allow preview versions to be downloaded")]
		[CommandOption("--preview")]
		public bool AllowPreviews { get; set; }

		[Description("Optional version to download")]
		[CommandOption("--version")]
		public string? Version { get; set; }

		[Description("Optional specific architecture (current host arch is default)")]
		[CommandOption("--arch")]
		public string? Architecture { get; set; }

		[Description("Optional specific OS (current host OS is default)")]
		[CommandOption("--os")]
		public string? OS { get; set; }

		public override ValidationResult Validate()
		{
			if (string.IsNullOrEmpty(Home))
				return ValidationResult.Error("--home is missing");

			if (!string.IsNullOrEmpty(OS))
			{
				if (OS != "windows" && OS != "linux" && OS != "macos")
					return ValidationResult.Error("--os must be one of: windows, linux, macos");
			}

			if (!string.IsNullOrEmpty(Architecture))
			{
				if (Architecture != "x86" && Architecture != "x64" && Architecture != "aarch" && Architecture != "aarch64")
					return ValidationResult.Error("--arch must be one of: x64, aarch64");
			}

			return ValidationResult.Success();
		}
	}

	public class SdkDownloadCommand : Command<SdkDownloadCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkDownloadCommandSettings settings)
		{
			if (string.IsNullOrEmpty(settings?.Home))
				throw new ArgumentException(nameof(settings.Home));

			(int? major, int? minor)? specificVersionToFind = null;
			if (!string.IsNullOrEmpty(settings.Version) && Version.TryParse(settings.Version, out var version))
				specificVersionToFind = (version.Major, version.Minor);

			try
			{
				var dir = new DirectoryInfo(settings.Home);

				if (settings.Force)
				{
					// Only delete cmdline-tools, not the entire SDK
					var cmdlineToolsDir = new DirectoryInfo(Path.Combine(dir.FullName, "cmdline-tools"));
					if (cmdlineToolsDir.Exists)
						cmdlineToolsDir.Delete(true);
				}
				else if (dir.Exists && SdkLocator.HasCmdlineTools(dir.FullName))
				{
					throw new InvalidOperationException(
						"cmdline-tools is already installed. Use --force to re-download.");
				}

				if (!dir.Exists)
					dir.Create();

				var tcsResult = new TaskCompletionSource<int>();

				var px = AnsiConsole.Progress();
				ProgressTask dlTask = null;

				var progress = 0;

				var downloader = new SdkDownloader();
				downloader.DownloadAsync(dir, specificVersionToFind, settings.AllowPreviews, settings.OS, settings.Architecture, progressHandler: (p) =>
				{
					progress = p;
				}).ContinueWith(t =>
				{
					dlTask?.StopTask();

					if (t.Exception is not null)
					{
						AnsiConsole.WriteException(t.Exception);
						tcsResult.TrySetResult(1);
					}
					else
					{
						tcsResult.TrySetResult(0);
					}
				});


				AnsiConsole.Progress()
				.Start(ctx =>
				{
					// Define tasks
					dlTask = ctx.AddTask("Downloading cmdline-tools...");

					while (!ctx.IsFinished)
					{
						dlTask.Value = progress;
					}
				});

				return tcsResult.Task.Result;
			}
			catch (SdkToolFailedExitException sdkEx)
			{
				Program.WriteException(sdkEx);
				return 1;
			}
		}
	}
}
