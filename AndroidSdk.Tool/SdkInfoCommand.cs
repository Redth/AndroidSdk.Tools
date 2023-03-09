﻿using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
        public string Home { get; set; }
    }

    public class SdkInfoResult
    {
        public string Path { get; set; }
        public string Version { get; set; }
        public bool IsUpToDate { get; set; }
        public string Channel { get; set; }
    }

    public class SdkInfoCommand : Command<SdkInfoCommandSettings>
    {
        public override int Execute([NotNull] CommandContext context, [NotNull] SdkInfoCommandSettings settings)
        {
            try
            {
                var m = new AndroidSdk.SdkManager(settings?.Home);
                m.SkipVersionCheck = true;

                var result = new SdkInfoResult();
                result.Path = m.AndroidSdkHome.FullName;
                result.Version = m.GetVersion().ToString();
                result.IsUpToDate= m.IsUpToDate();
                result.Channel = m.Channel.ToString();

                var jdks = m.Jdks;

                if (settings.Format == OutputFormat.None)
                {
                    var rule = new Rule("SDK Info:");
                    rule.Centered();
                    AnsiConsole.Write(rule);

                    OutputHelper.OutputObject<SdkInfoResult>(
                        result,
                        new[] { "Path", "Version", "IsUpToDate", "Channel" },
                        i => new[] { i.Path, i.Version, i.IsUpToDate.ToString(), i.Channel});


                    if (jdks is not null && jdks.Length > 0)
                    {
                        rule = new Rule("JDK Info:");
                        rule.Centered();
                        AnsiConsole.Write(rule);

                        OutputHelper.OutputTable<JdkInfo>(
                            jdks,
                            new[] { "Version", "Path", "Java Path", "JavaC Path" },
                            i => new[] { i.Version.ToString(), i.Home.FullName, i.Java.FullName, i.JavaC.FullName});
                    }

                }
                else
                {
                    var objr = new { SdkInfo = result, Jdks = jdks };
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
