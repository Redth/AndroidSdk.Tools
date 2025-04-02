﻿using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AndroidSdk.Tool
{
	public class SdkLicenseCommandSettings : CommandSettings
	{
		[Description("Output Format")]
		[CommandOption("-f|--format")]
		[DefaultValue(OutputFormat.None)]
		[TypeConverter(typeof(OutputFormatTypeConverter))]
		public OutputFormat Format { get; set; }

		[Description("Android SDK Home/Root Path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("Only accepted licenses")]
		[CommandOption("-a|--accepted")]
		public bool Accepted { get; set; }

		[Description("Only unaccepted licenses")]
		[CommandOption("-u|--unaccepted")]
		public bool UnAccepted { get; set; }
	}

	public class SdkLicenseInfo
	{
		public string Id { get; set; }
		public string LicenseText { get; set; }
		public bool IsAccepted { get; set; }
	}

	public class SdkLicenseCommand : Command<SdkLicenseCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] SdkLicenseCommandSettings settings)
		{
			try
			{
				var dotnetPreferredPaths = MonoDroidSdkLocator.LocatePaths();

				var m = new AndroidSdk.SdkManager(settings?.Home);
				m.SkipVersionCheck = true;

				var allLicenses = m.GetLicenses().Select(l => 
					new SdkLicenseInfo
					{
						Id = l.Id,
						LicenseText = string.Join(Environment.NewLine, l.License),
						IsAccepted = l.Accepted
					}).ToList();

				var licenses = new List<SdkLicenseInfo>();

				if (!settings.Accepted && !settings.UnAccepted)
					licenses.AddRange(allLicenses);

				if (settings.Accepted)
					licenses.AddRange(allLicenses.Where(l => l.IsAccepted));
				if (settings.UnAccepted)
					licenses.AddRange(allLicenses.Where(l => !l.IsAccepted));

				if (settings.Format == OutputFormat.None)
				{
					foreach (var l in licenses)
					{
						var accepted = l.IsAccepted ? "Accepted" : "Not Accepted";
						var rule = new Rule($"License: {l.Id} ({accepted})");
						rule.Centered();
						AnsiConsole.Write(rule);
						AnsiConsole.WriteLine(l.LicenseText);
					}
				}
				else
				{
					var objr = new
					{
						Licenses = licenses,
					};
					OutputHelper.Output(objr, settings.Format);
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
