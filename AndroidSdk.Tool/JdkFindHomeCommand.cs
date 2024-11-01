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
	public class JdkFindHomeCommandSettings : CommandSettings
	{
		[Description("Filter returned versions based on a NuGet syntax version or version range")]
		[CommandOption("-v|--version <VERSION>")]
		public string? VersionRange { get; set; }
	}

	public class JdkFindHomeCommand : Command<JdkFindHomeCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] JdkFindHomeCommandSettings settings)
		{
			try
			{
				var supportedJdkVersionRange = new VersionRange(new NuGetVersion(17, 0, 0));
				if (!string.IsNullOrEmpty(settings.VersionRange) && VersionRange.TryParse(settings.VersionRange, out var vr))
					supportedJdkVersionRange = vr;

				var j = new AndroidSdk.JdkLocator();
				var jdks = j.LocateJdk();

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
				var foundJdk = jdkList.OrderByDescending(j => j.Version).ToList().FirstOrDefault();

				if (foundJdk is not null)
					AnsiConsole.WriteLine(foundJdk.Home);
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
