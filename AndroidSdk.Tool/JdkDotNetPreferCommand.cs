using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AndroidSdk.Tool
{
	public class JdkDotNetPreferCommandSettings : CommandSettings
	{
		[Description("Java JDK Home/Root Path")]
		[CommandOption("-h|--home <PATH>")]
		public string Home { get; set; }
	}
	
	public class JdkDotNetPreferCommand : Command<JdkDotNetPreferCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] JdkDotNetPreferCommandSettings settings)
		{
			try
			{
				var jdkLocator = new JdkLocator();
				var jdks = jdkLocator.LocateJdk(settings.Home, returnOnlySpecified: true);
				var jdk = jdks.FirstOrDefault();

				if (jdk != null)
				{
					MonoDroidSdkLocator.UpdatePaths(new MonoDroidSdkLocation(null, javaJdkPath: jdk.Home.FullName));
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