#nullable enable
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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
	public DirectoryInfo? Home { get; set; }

	[Description("Java JDK Home Path")]
	[CommandOption("-j|--jdk")]
	public DirectoryInfo? JdkHome { get; set; }
}

public abstract class BaseDeviceCommand<TSettings> : Command<TSettings>
	where TSettings : BaseDeviceCommandSettings
{
	public abstract int Execute([NotNull] CommandContext context, [NotNull] TSettings settings, [NotNull] Adb adb);

	public override int Execute([NotNull] CommandContext context, [NotNull] TSettings settings)
	{
		try
		{
			var sdk = new AndroidSdkManager(settings.Home, settings.JdkHome);
			
			return Execute(context, settings, sdk.Adb);
		}
		catch (SdkToolFailedExitException sdkEx)
		{
			Program.WriteException(sdkEx);
			return 1;
		}
	}
}
