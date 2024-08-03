#nullable enable
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool;

public class BaseDeviceCommandSettings : CommandSettings
{
	[Description("Output Format")]
	[CommandOption("-f|--format")]
	[DefaultValue(OutputFormat.None)]
	[TypeConverter(typeof(OutputFormatTypeConverter))]
	public OutputFormat Format { get; set; }

	[Description("Android SDK Home/Root Path")]
	[CommandOption("-h|--home")]
	public string? Home { get; set; }
}

public abstract class BaseDeviceCommand<TSettings> : Command<TSettings>
	where TSettings : BaseDeviceCommandSettings
{
	public abstract int Execute([NotNull] CommandContext context, [NotNull] TSettings settings, [NotNull] Adb adb);

	public override int Execute([NotNull] CommandContext context, [NotNull] TSettings settings)
	{
		try
		{
			var adb = new Adb(settings.Home);
			return Execute(context, settings, adb);
		}
		catch (SdkToolFailedExitException sdkEx)
		{
			Program.WriteException(sdkEx);
			return 1;
		}
	}
}
