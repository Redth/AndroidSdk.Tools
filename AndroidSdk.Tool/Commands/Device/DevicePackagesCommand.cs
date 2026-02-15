#nullable enable
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace AndroidSdk.Tool;

public class DevicePackagesCommandSettings : DeviceCommandSettings
{
}

public class DevicePackagesCommand : SingleDeviceCommand<DevicePackagesCommandSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] DevicePackagesCommandSettings settings, [NotNull] Adb adb, [NotNull] Adb.AdbDevice device)
	{
		var pm = new PackageManager(adb.AndroidSdkHome, device.Serial);
		var packages = pm.ListPackages();

		if (settings.Format == OutputFormat.None)
		{
			OutputHelper.OutputTable(
				packages,
				new[] { "Package Name", "Installer", "Install Path" },
				i => new[] { i.PackageName, i.Installer ?? "", i.InstallPath?.FullName ?? "" });
		}
		else
		{
			var items = packages.Select(p => new PackageInfo
			{
				PackageName = p.PackageName,
				Installer = p.Installer,
				InstallPath = p.InstallPath?.FullName
			}).ToList();
			OutputHelper.Output(items, settings.Format);
		}

		return 0;
	}

	[DataContract]
	internal class PackageInfo
	{
		[DataMember(Name = "packageName")]
		public string PackageName { get; set; } = null!;

		[DataMember(Name = "installer")]
		public string? Installer { get; set; }

		[DataMember(Name = "installPath")]
		public string? InstallPath { get; set; }
	}
}
