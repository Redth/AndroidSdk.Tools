using System;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
				var dotnetPreferredPaths = MonoDroidSdkLocator.LocatePaths();

				var m = new AndroidSdk.SdkManager();
				m.SkipVersionCheck = true;

				if (!string.IsNullOrEmpty(m?.AndroidSdkHome?.FullName))
				{
					AnsiConsole.WriteLine(m.AndroidSdkHome.FullName);
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
