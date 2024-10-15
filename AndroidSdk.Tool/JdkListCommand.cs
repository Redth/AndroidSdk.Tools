using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AndroidSdk.Tool
{
	public class JdkListCommandSettings : CommandSettings
	{
		[Description("Output Format")]
		[CommandOption("-f|--format")]
		[DefaultValue(OutputFormat.None)]
		[TypeConverter(typeof(OutputFormatTypeConverter))]
		public OutputFormat Format { get; set; }

		[Description("Java JDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("Additional JDK Home Paths to search")]
		[CommandOption("-p|--path")]
		public string[] AdditionalPaths { get; set; }
	}

	record JdkInfoOutput(string Version, string Home, string Java, string JavaC, bool DotNetPreferred, bool SetByEnvironmentVariable);

	public class JdkListCommand : Command<JdkListCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] JdkListCommandSettings settings)
		{
			try
			{
				var j = new AndroidSdk.JdkLocator();
				var jdks = j.LocateJdk(settings.Home, settings.AdditionalPaths);

				var jdkList = jdks.Select(j => new JdkInfoOutput(j.Version, j.Home.FullName, j.Java.FullName, j.JavaC.FullName, j.PreferredByDotNet, j.SetByEnvironmentVariable)).ToList();

				OutputHelper.Output<JdkInfoOutput>(
					jdkList,
					settings.Format,
					[ "Version", "Path", "Java", "JavaC", "DotNet Preferred", "From Env Var" ],
					i => [ i.Version, i.Home, i.Java, i.JavaC, i.SetByEnvironmentVariable.ToString() ]);
			}
			catch (Exception sdkEx)
			{
				AnsiConsole.WriteException(sdkEx);
				return 1;
			}

			return 0;
		}
	}
}
