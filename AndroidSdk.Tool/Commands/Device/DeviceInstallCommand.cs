#nullable enable
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace AndroidSdk.Tool;

public class DeviceInstallCommandSettings : DeviceCommandSettings
{
	[Description("The package to install")]
	[CommandOption("-p|--pkg|--package")]
	public string Package { get; set; } = null!;

	public override ValidationResult Validate()
	{
		if (string.IsNullOrWhiteSpace(Package))
			return ValidationResult.Error("Missing --package");

		if (!File.Exists(Package))
			return ValidationResult.Error($"Package {Package} was not found");

		return ValidationResult.Success();
	}
}

public class DeviceInstallCommand : SingleDeviceCommand<DeviceInstallCommandSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] DeviceInstallCommandSettings settings, [NotNull] Adb adb, [NotNull] Adb.AdbDevice device)
	{
		var package = new FileInfo(settings.Package);

		adb.Install(package, device.Serial);

		return 0;
	}
}
