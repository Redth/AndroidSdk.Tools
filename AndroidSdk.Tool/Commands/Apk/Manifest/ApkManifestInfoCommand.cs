#nullable enable
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace AndroidSdk.Tool;

public class ApkManifestInfoCommandSettings : CommandSettings
{
	[Description("Output Format")]
	[CommandOption("-f|--format")]
	[DefaultValue(OutputFormat.None)]
	[TypeConverter(typeof(OutputFormatTypeConverter))]
	public OutputFormat Format { get; set; }

	[Description("The package to read")]
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

public class ApkManifestInfoCommand : Command<ApkManifestInfoCommandSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] ApkManifestInfoCommandSettings settings)
	{
		try
		{
			var reader = new Apk.ApkReader(settings.Package);
			var androidManifest = reader.ReadManifest();
			var manifest = androidManifest.Manifest;

			if (settings.Format == OutputFormat.Xml)
			{
				AnsiConsole.WriteLine(androidManifest.ManifestElement.ToString());
			}
			else if (settings.Format == OutputFormat.Json)
			{
				AnsiConsole.WriteLine(JsonConvert.SerializeXNode(androidManifest.ManifestElement, Formatting.Indented));
			}
			else
			{
				OutputHelper.OutputObject(
					manifest,
					new[]
					{
						"Package ID",
						"Version Code",
						"Version Name",
						"Minimum SDK",
						"Target SDK",
						"Maximum SDK"
					},
					i => new string[]
					{
						i.PackageId,
						i.VersionCode.ToString("0", CultureInfo.InvariantCulture),
						i.VersionName,
						(i.UsesSdk?.MinSdkVersion ?? 0) == 0 ? "" : (i.UsesSdk?.MinSdkVersion?.ToString("0", CultureInfo.InvariantCulture) ?? ""),
						(i.UsesSdk?.TargetSdkVersion ?? 0) == 0 ? "" : (i.UsesSdk?.TargetSdkVersion?.ToString("0", CultureInfo.InvariantCulture) ?? ""),
						(i.UsesSdk?.MaxSdkVersion ?? 0) == 0 ? "" : (i.UsesSdk ?.MaxSdkVersion ?.ToString("0", CultureInfo.InvariantCulture) ?? "")
					});
			}
		}
		catch (SdkToolFailedExitException sdkEx)
		{
			Program.WriteException(sdkEx);
			return 1;
		}

		return 0;
	}
}
