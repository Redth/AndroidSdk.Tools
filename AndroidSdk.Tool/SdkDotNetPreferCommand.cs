using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AndroidSdk.Tool
{
	public class SdkDotNetPreferCommandSettings : CommandSettings
	{
		[Description("Android SDK Home Path")]
		[CommandOption("-h|--home <PATH>")]
		public string Home { get; set; }
	}
	
	public class SdkDotNetPreferCommand : Command<SdkDotNetPreferCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkDotNetPreferCommandSettings settings)
		{
			try
			{
				var sdkLocator = new SdkLocator();
				var sdk = sdkLocator.Locate(settings.Home)?.FirstOrDefault();

				if (sdk != null)
				{
					MonoDroidSdkLocator.UpdatePaths(new MonoDroidSdkLocation(sdk.Root.FullName, null));
					return 0;
				}
			}
			catch (Exception sdkEx)
			{
				AnsiConsole.WriteException(sdkEx);
			}

			return 1;
		}
	}
}