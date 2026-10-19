#nullable enable
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AndroidSdk.Tool;

public class AvdStopCommandSettings : CommandSettings
{
	[CommandOption("-n|--name")]
	[Description("Exact name of an emulator started by this tool")]
	public string? Name { get; set; }

	[CommandOption("--serial")]
	[Description("Exact serial returned by avd start (also guards --name when both are supplied)")]
	public string? Serial { get; set; }

	[CommandOption("-h|--home")]
	[Description("Android SDK home/root path used by avd start")]
	public string? Home { get; set; }

	[CommandOption("-t|--timeout")]
	[Description("Maximum seconds to connect, stop, await termination and flush logs")]
	[DefaultValue(30)]
	public int Timeout { get; set; } = 30;

	public override ValidationResult Validate()
	{
		if (string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Serial))
			return ValidationResult.Error("Missing --name or --serial");
		if (Serial != null && (!Serial.StartsWith("emulator-", StringComparison.Ordinal) ||
			!int.TryParse(Serial.Substring(9), out var port) || port < 5554 || port > 5682 || port % 2 != 0))
			return ValidationResult.Error("--serial must be an exact emulator serial");
		return Timeout > 0 ? ValidationResult.Success() : ValidationResult.Error("--timeout must be positive");
	}
}

public class AvdStopCommand : CancellableAsyncCommand<AvdStopCommandSettings>
{
	public override async Task<int> ExecuteAsync(CommandContext context, AvdStopCommandSettings settings, CancellationToken cancellation)
	{
		try
		{
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			timeout.CancelAfter(TimeSpan.FromSeconds(settings.Timeout));
			await AvdSession.StopAsync(settings.Home, settings.Name, settings.Serial, null, timeout.Token);
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex);
			return 1;
		}
	}
}
