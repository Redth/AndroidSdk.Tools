#nullable enable
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

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
		string intentArgs;

		if (!string.IsNullOrEmpty(settings.Activity))
		{
			intentArgs = $"{settings.Package}/{settings.Activity}";
		}
		else
		{
			if (adb.LaunchPackage(settings.Package, device.Serial))
			{
				AnsiConsole.MarkupLine($"[green]Launched {settings.Package}[/]");
				return 0;
			}

			AnsiConsole.MarkupLine($"[red]Failed to launch {settings.Package}[/]");
			return 1;
		}

		var am = new ActivityManager(adb.AndroidSdkHome?.FullName, device.Serial);
		var success = am.StartActivity(intentArgs, new ActivityManager.ActivityManagerStartOptions
		{
			WaitForLaunch = settings.WaitForLaunch,
		});

		if (success)
		{
			AnsiConsole.MarkupLine($"[green]Launched {intentArgs}[/]");
		}
		else
		{
			AnsiConsole.MarkupLine($"[red]Failed to launch {intentArgs}[/]");
		}

		return success ? 0 : 1;
	}
}
