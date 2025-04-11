using System;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.IO;

namespace AndroidSdk.Tool
{
    public class SdkInfoCommandSettings : CommandSettings
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

    public class SdkInfoResult(string? path, string? version, bool isUpToDate, string? channel, bool dotNetPreferred, bool writeAccess)
    {
        public string? Path { get; set; } = path;
		public string? Version { get; set; } = version;
        public bool IsUpToDate { get; set; } = isUpToDate;
		public string? Channel { get; set; } = channel;
        
		public bool WriteAccess { get; set; } = writeAccess;

		public bool DotNetPreferred { get; set; } = dotNetPreferred;
	}

    public class SdkInfoCommand : Command<SdkInfoCommandSettings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] SdkInfoCommandSettings settings)
        {
            try
            {
	            var dotnetPreferredPaths = MonoDroidSdkLocator.LocatePaths();

				var sdk = new AndroidSdkManager(settings.Home, settings.JdkHome);

                sdk.SdkManager.SkipVersionCheck = true;

				var foundSdk = sdk.SdkManager.AndroidSdkHome is not null;

                var sep = System.IO.Path.PathSeparator;
                
                var infoPath = sdk.Home?.FullName;
                var infoVersion = sdk.SdkManager.GetVersion()?.ToString();
                var infoIsUpToDate = foundSdk && sdk.SdkManager.IsUpToDate();
                var infoChannel = foundSdk ? sdk.SdkManager.Channel.ToString() : null;
                var infoDotNetPreferred = OperatingSystem.IsWindows()
					? sdk.Home?.FullName.TrimEnd(sep).Equals(dotnetPreferredPaths.AndroidSdkPath?.TrimEnd(sep)) ?? false
					: sdk.Home?.FullName.ToLower().TrimEnd(sep).Equals(dotnetPreferredPaths.AndroidSdkPath?.ToLower()?.TrimEnd(sep)) ?? false;

				var infoWriteAccess = false;
				try
				{
					infoWriteAccess = sdk.SdkManager.CanModify();
				} catch { }

				var result = new SdkInfoResult(infoPath, infoVersion, infoIsUpToDate, infoChannel, infoDotNetPreferred, infoWriteAccess);


				var jdks = new JdkLocator().LocateJdk();

                if (settings.Format == OutputFormat.None)
                {
                    var rule = new Rule("SDK Info:");
                    rule.Centered();
                    AnsiConsole.Write(rule);

                    OutputHelper.OutputObject<SdkInfoResult>(
                        result,
                        [ "Path", "Version", "IsUpToDate", "Channel", "DotNetPreferred", "WriteAccess" ],
                        i => [i.Path ?? string.Empty, i.Version ?? string.Empty, i.IsUpToDate.ToString(), i.Channel ?? string.Empty, i.DotNetPreferred.ToString(), i.WriteAccess.ToString()]);


                    if (jdks is not null && jdks.Count() > 0)
                    {
                        rule = new Rule("JDK Info:");
                        rule.Centered();
                        AnsiConsole.Write(rule);

                        OutputHelper.OutputTable<JdkInfo>(
                            jdks,
                            [ "Version", "Path", "Java Path", "JavaC Path" ],
                            i => [ i.Version.ToString(), i.Home.FullName, i.Java.FullName, i.JavaC.FullName ]);
                    }

                }
                else
                {
                    var objr = new { SdkInfo = result, Jdks = (jdks ?? []).Select(j => new
                    {
	                    Version = j.Version.ToString(),
	                    Path = j.Home.FullName,
	                    JavaPath = j.Java.FullName,
	                    JavaCPath = j.JavaC.FullName,
	                    DotNetPreferred = j.PreferredByDotNet,
	                    SetByEnvironmentVariable = j.SetByEnvironmentVariable,
                    }) };
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
