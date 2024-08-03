#nullable enable
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool;

public class DeviceUninstallCommandSettings : DeviceCommandSettings
{
	[Description("The package to uninstall")]
	[CommandOption("-p|--pkg|--package")]
	public string Package { get; set; } = null!;

	[Description("Keep the data and cache directories")]
	[CommandOption("-k|--keep-data")]
	public bool KeepData { get; set; }

	public override ValidationResult Validate()
	{
		if (string.IsNullOrWhiteSpace(Package))
			return ValidationResult.Error("Missing --package");

		return ValidationResult.Success();
	}
}

public class DeviceUninstallCommand : SingleDeviceCommand<DeviceUninstallCommandSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] DeviceUninstallCommandSettings settings, [NotNull] Adb adb, [NotNull] Adb.AdbDevice device)
	{
		adb.Uninstall(settings.Package, settings.KeepData, device.Serial);

		return 0;
	}
}
