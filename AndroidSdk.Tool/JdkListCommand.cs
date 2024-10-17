using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NuGet.Versioning;

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
		[CommandOption("-h|--home <HOME>")]
		public string Home { get; set; }

		[Description("Additional JDK Home Paths to search")]
		[CommandOption("-p|--path <PATH>")]
		public string[] AdditionalPaths { get; set; }
		
		[Description("Filter returned versions based on a NuGet syntax version or version range")]
		[CommandOption("-v|--version <VERSION>")]
		public string? VersionRange { get; set; }
	}

	record JdkInfoOutput(NuGetVersion Version, string Home, string Java, string JavaC, bool DotNetPreferred, bool SetByEnvironmentVariable);

	public class JdkListCommand : Command<JdkListCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] JdkListCommandSettings settings)
		{
			try
			{
				var supportedJdkVersionRange = new VersionRange(new NuGetVersion(17, 0,0));
				if (!string.IsNullOrEmpty(settings.VersionRange) && VersionRange.TryParse(settings.VersionRange, out var vr))
					supportedJdkVersionRange = vr;

				var j = new AndroidSdk.JdkLocator();
				var jdks = j.LocateJdk(settings.Home, settings.AdditionalPaths);
				
				var jdkList = new List<JdkInfoOutput>();

				foreach (var jdk in jdks)
				{
					if (!NuGetVersion.TryParse(jdk.Version, out var jdkVersion))
						continue;

					if (!supportedJdkVersionRange.Satisfies(jdkVersion))
						continue;
					
					var jdkInfo = new JdkInfoOutput(jdkVersion, jdk.Home.FullName, jdk.Java.FullName,
						jdk.JavaC.FullName, jdk.PreferredByDotNet, jdk.SetByEnvironmentVariable);
					
					jdkList.Add(jdkInfo);
				}
				
				// Sort by newest first
				jdkList = jdkList.OrderByDescending(j => j.Version).ToList();
				
				OutputHelper.Output<JdkInfoOutput>(
					jdkList,
					settings.Format,
					[ "Version", "Path", "Java", "JavaC", "DotNet Preferred", "From Env Var" ],
					i => [ i.Version.ToNormalizedString(), i.Home, i.Java, i.JavaC, i.DotNetPreferred.ToString(), i.SetByEnvironmentVariable.ToString() ]);
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
