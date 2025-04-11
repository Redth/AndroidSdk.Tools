using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace AndroidSdk.Tool
{
	public class AvdCreateCommandSettings : CommandSettings
	{
		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public DirectoryInfo? Home { get; set; }

		[Description("Java JDK Home Path")]
		[CommandOption("-j|--jdk")]
		public DirectoryInfo? JdkHome { get; set; }

		[Description("Name of the AVD/Emulator")]
		[CommandOption("-n|--name")]
		public string? Name { get; set; }

		[Description("SDK Id (Possible values from: `sdk list`)")]
		[CommandOption("-s|--sdk|--sdkid")]
		public string? SdkId { get; set; }

		[Description("Device Id or NumericId (Possible options from: `avd devices`)")]
		[CommandOption("-d|--device")]
		public string? DeviceId { get; set; }

		[Description("Target Id or NumericId (Possible options from: `avd targets`)")]
		[CommandOption("-t|--target")]
		public string? TargetId { get; set; }

		[Description("Path to create the AVD")]
		[CommandOption("-p|--path")]
		public string? Path { get; set; }

		[Description("Path to create the SDCard")]
		[CommandOption("--sdcard-path")]
		public string? SdCardPath { get; set; }

		[Description("Size of SDCard to create in MB")]
		[CommandOption("--sdcard-size")]
		public int? SdCardSizeMb { get; set; }
		
		[Description("The ABI to use for the AVD (Auto-selects if there is only one ABI)")]
		[CommandOption("-a|--abi")]
		public string? Abi { get; set; }
		
		[Description("The name of a skin to use with this device.")]
		[CommandOption("--skin")]
		public string? Skin { get; set; }

		[Description("Force the creation of the AVD")]
		[CommandOption("-f|--force")]
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
				var sdk = new AndroidSdkManager(settings.Home, settings.JdkHome);

				string? sdcard = null;
				if (!string.IsNullOrEmpty(settings.SdCardPath))
					sdcard = settings.SdCardPath;
				else if (settings.SdCardSizeMb.HasValue)
					sdcard = settings.SdCardSizeMb.ToString() + "MB";

				var options = new AvdManager.AvdCreateOptions
				{
					Device = settings.DeviceId,
					Path = settings.Path,
					Force = settings.Force,
					SdCardPathOrSize = sdcard,
					Abi = settings.Abi,
					Skin = settings.Skin
				};

				sdk.AvdManager.Create(
					settings.Name!,
					settings.SdkId!,
					options);
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
