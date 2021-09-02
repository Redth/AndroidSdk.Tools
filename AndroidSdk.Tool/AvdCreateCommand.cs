using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool
{
	public class AvdCreateCommandSettings : CommandSettings
	{
		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("Name of the AVD/Emulator")]
		[CommandOption("-n|--name")]
		public string Name { get; set; }

		[Description("SDK Id (Possible values from: `sdk list`)")]
		[CommandOption("-s|--sdk|--sdkid")]
		public string SdkId { get; set; }

		[Description("Device Id or NumericId (Possible options from: `avd devices`)")]
		[CommandOption("-d|--device")]
		public string DeviceId { get; set; }

		[Description("Target Id or NumericId (Possible options from: `avd targets`)")]
		[CommandOption("-t|--target")]
		public string TargetId { get; set; }

		[Description("Path to create the AVD")]
		[CommandOption("-p|--path")]
		public string Path { get; set; }

		[Description("Path to create the SDCard")]
		[CommandOption("--sdcard-path")]
		public string SdCardPath { get; set; }

		[Description("Size of SDCard to create in megabytes")]
		[CommandOption("--sdcard-size")]
		public int? SdCardSizeMb { get; set; }

		[Description("Force the creation of the AVD")]
		[CommandOption("--force")]
		public bool Force { get; set; }

		public override ValidationResult Validate()
		{
			if (string.IsNullOrEmpty(Name))
				return ValidationResult.Error("Missing --name");

			if (string.IsNullOrEmpty(SdkId))
				return ValidationResult.Error("Missing --sdkid");

			return ValidationResult.Success();
		}
	}

	public class AvdCreateCommand : Command<AvdCreateCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdCreateCommandSettings settings)
		{
			try
			{
				var avd = new AvdManager(settings?.Home);

				avd.Create(
					settings.Name,
					settings.SdkId,
					settings.DeviceId,
					settings.Path,
					settings.Force,
					settings.SdCardPath,
					settings.SdCardSizeMb.HasValue ? (settings.SdCardSizeMb.ToString() + "MB") : (string)null);
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
