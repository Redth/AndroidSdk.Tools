﻿using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AndroidSdk.Tool
{
	public class SdkAcceptLicenseCommandSettings : CommandSettings
	{
		[Description("Output Format")]
		[CommandOption("-f|--format")]
		[DefaultValue(OutputFormat.None)]
		[TypeConverter(typeof(OutputFormatTypeConverter))]
		public OutputFormat Format { get; set; }

		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("Force license accept without display license text or requiring any user interaction")]
		[CommandOption("--force")]
		public bool Force { get; set; }
	}

	public class SdkAcceptLicenseCommand : Command<SdkAcceptLicenseCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkAcceptLicenseCommandSettings settings)
		{
			try
			{
				var dotnetPreferredPaths = MonoDroidSdkLocator.LocatePaths();

				var m = new AndroidSdk.SdkManager(settings?.Home);
				m.SkipVersionCheck = true;

				var licenses = m.GetLicenses().Select(l =>
					new SdkLicenseInfo
					{
						Id = l.Id,
						LicenseText = string.Join(Environment.NewLine, l.License),
						IsAccepted = l.Accepted
					}).Where(l => !l.IsAccepted).ToList();

				var doAccept = true;

				if (!settings.Force)
				{
					foreach (var l in licenses)
					{
						var rule = new Rule($"License: {l.Id}");
						rule.Centered();
						AnsiConsole.Write(rule);
						AnsiConsole.WriteLine(l.LicenseText);
						var answer = AnsiConsole.Confirm($"Do you Accept License '{l.Id}'?", false);

						if (!answer)
						{
							doAccept = false;
						}
					}
				}

				if (doAccept)
				{
					m.AcceptLicenses();
					return 0;
				}
			}
			catch (SdkToolFailedExitException sdkEx)
			{
				Program.WriteException(sdkEx);
				return 1;
			}

			return 1;
		}
	}
}
