#nullable enable
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AndroidSdk.Tool;

public class DeviceLaunchCommandSettings : DeviceCommandSettings
{
	[Description("Package name to launch (e.g., com.companyname.myapp)")]
	[CommandOption("-p|--pkg|--package")]
	public string Package { get; set; } = null!;

	[Description("Activity name to launch (optional - uses default launcher activity if not specified)")]
	[CommandOption("-a|--activity")]
	public string? Activity { get; set; }

	[Description("Wait for the activity to finish launching")]
	[CommandOption("-w|--wait")]
	[DefaultValue(true)]
	public bool WaitForLaunch { get; set; }

	public override ValidationResult Validate()
	{
		if (string.IsNullOrWhiteSpace(Package))
			return ValidationResult.Error("Missing --package");

		return ValidationResult.Success();
	}
}

public class DeviceLaunchCommand : SingleDeviceCommand<DeviceLaunchCommandSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] DeviceLaunchCommandSettings settings, [NotNull] Adb adb, [NotNull] Adb.AdbDevice device)
	{
		bool success;
		string intentArgs;
		if (string.IsNullOrEmpty(settings.Activity))
		{
			intentArgs = settings.Package;
			var output = adb.LaunchApp(intentArgs, device.Serial);
			success = output.Any(l => l.Contains("Events injected", StringComparison.OrdinalIgnoreCase));
		}
		else
		{
			intentArgs = $"{settings.Package}/{settings.Activity}";
			var am = new ActivityManager(adb.AndroidSdkHome?.FullName, device.Serial);
			success = am.StartActivity(intentArgs, new ActivityManager.ActivityManagerStartOptions
			{
				WaitForLaunch = settings.WaitForLaunch,
			});
		}

		if (success)
		{
			AnsiConsole.MarkupLine($"[green]Launched {intentArgs}[/]");
			return 0;
		}

		AnsiConsole.MarkupLine($"[red]Failed to launch {intentArgs}[/]");
		return 1;
	}
}
