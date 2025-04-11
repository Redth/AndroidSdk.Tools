using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace AndroidSdk.Tool
{
	public class SdkFindCommandSettings : CommandSettings
	{
	}

	public class SdkFindCommand : Command<SdkFindCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkFindCommandSettings settings)
		{
			try
			{
				var sdk = new AndroidSdkManager();
				sdk.SdkManager.SkipVersionCheck = true;

				if (!string.IsNullOrEmpty(sdk.Home?.FullName))
				{
					AnsiConsole.WriteLine(sdk.Home.FullName);
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
}
