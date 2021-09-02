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

		[Description("Forces install even if directory already exists and is not empty")]
		[CommandOption("-f|--force")]
		public bool Force { get; set; }

		public override ValidationResult Validate()
		{
			if (string.IsNullOrEmpty(Home))
				return ValidationResult.Error("--home is missing");
			return ValidationResult.Success();
		}
	}

	public class SdkDownloadCommand : Command<SdkDownloadCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkDownloadCommandSettings settings)
		{
			if (string.IsNullOrEmpty(settings?.Home))
				throw new ArgumentException(nameof(settings.Home));

			try
			{
				var dir = new DirectoryInfo(settings.Home);

				if (settings.Force)
				{
					if (dir.Exists)
						dir.Delete(true);
				}

				if (dir.Exists && ((dir.GetDirectories()?.Any() ?? false) || (dir.GetFiles()?.Any() ?? false)))
					throw new InvalidOperationException("Directory already exists and is not empty!");

				if (!dir.Exists)
					dir.Create();

				var m = new SdkManager(dir);

				m.FindToolPath(new DirectoryInfo(settings.Home));

				var tcsResult = new TaskCompletionSource<int>();

				var px = AnsiConsole.Progress();
				ProgressTask dlTask = null;

				var progress = 0;

				m.DownloadSdk(dir, progressHandler: (p) =>
					{
						progress = p;
					}).ContinueWith(t =>
					{
						dlTask?.StopTask();
						tcsResult.TrySetResult(0);
					});

				AnsiConsole.Progress()
				.Start(ctx =>
				{
					// Define tasks
					dlTask = ctx.AddTask("Downloading Android SDK...");

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
