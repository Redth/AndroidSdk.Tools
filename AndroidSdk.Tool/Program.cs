using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidSdk.Tool
{
	class Program
	{
		public static void Main(string[] args)
		{
			var app = new CommandApp();
			app.Configure(config =>
			{
				config.AddBranch("sdk", sdkBranch =>
				{
					sdkBranch.AddCommand<SdkListCommand>("list")
						.WithDescription("Lists Android SDK packages")
						.WithExample(new[] { "sdk", "list" })
						.WithExample(new[] { "sdk", "list", "--available" })
						.WithExample(new[] { "sdk", "list", "--installed" });
					sdkBranch.AddCommand<SdkInstallCommand>("install")
						.WithDescription("Installs and/or Updates the specified Android SDK package(s)")
						.WithExample(new[] { "sdk", "install", "--package emulator" });
					sdkBranch.AddCommand<SdkUninstallCommand>("uninstall")
					.WithDescription("Uninstalls the specified Android SDK package(s)")
						.WithExample(new[] { "sdk", "uninstall", "--package emulator" });
					sdkBranch.AddCommand<SdkDownloadCommand>("download")
						.WithDescription("Downloads a new copy of the Android SDK cmdline-tools")
						.WithExample(new[] { "sdk", "download", "--home /path/to/sdk" })
						.WithExample(new[] { "sdk", "download", "--home /path/to/sdk", "--force" });
					sdkBranch.AddCommand<SdkInfoCommand>("info")
						.WithDescription("Android SDK Info")
						.WithExample(new[] { "sdk", "info" });
				});

				config.AddBranch("device", sdkBranch =>
				{
					sdkBranch.AddCommand<DevicesListCommand>("list")
						.WithDescription("Lists available devices / emulators")
						.WithExample(new[] { "device", "list" });
					sdkBranch.AddCommand<DeviceInfoCommand>("info")
						.WithDescription("Lists device properties")
						.WithExample(new[] { "device", "info", "--property ro.product.system.model" })
						.WithExample(new[] { "device", "info", "--property \"ro\\.vendor\\..*\"" })
						.WithExample(new[] { "device", "info", "--device 172.22.100.90" })
						.WithExample(new[] { "device", "info", "--device emulator.*" })
						.WithExample(new[] { "device", "info", "--device emulator.* --property ro.product.cpu.abi" });
				});

				config.AddBranch("avd", sdkBranch =>
				{
					sdkBranch.AddCommand<AvdListCommand>("list")
						.WithDescription("Lists available AVDs")
						.WithExample(new[] { "avd", "list" });
					sdkBranch.AddCommand<AvdTargetsCommand>("targets")
						.WithDescription("Lists available targets for AVDs")
						.WithExample(new[] { "avd", "targets" });
					sdkBranch.AddCommand<AvdDevicesCommand>("devices")
						.WithDescription("Lists available devices for AVDs")
						.WithExample(new[] { "avd", "devices" });
					sdkBranch.AddCommand<AvdCreateCommand>("create")
						.WithDescription("Creates a new AVD")
						.WithExample(new[] { "avd", "create", "--name MyEmulator", "--sdk \"system-images;android-31;google_apis;x86_64\"", "--device pixel" });
					sdkBranch.AddCommand<AvdDeleteCommand>("delete")
						.WithDescription("Deletes an AVD")
						.WithExample(new[] { "avd", "delete", "--name MyEmulator" });
					sdkBranch.AddCommand<AvdStartCommand>("start")
						.WithDescription("Starts an existing available AVD by name")
						.WithExample(new[] { "avd", "start", "--name MyEmulator" })
						.WithExample(new[] { "avd", "start", "--name MyEmulator", "--wait-boot" })
						.WithExample(new[] { "avd", "start", "--name MyEmulator", "--wait-boot", "--no-snapshot" });
				});
			});

			try
			{
				app.Run(args);
			}
			catch (Exception ex)
			{
				AnsiConsole.WriteException(ex);
			}
		}

		internal static void WriteException(SdkToolFailedExitException sdkEx)
		{
			foreach (var line in sdkEx.StdErr)
			{
				if (line.StartsWith("Picked up JAVA_TOOL_OPTIONS:"))
					continue;

				AnsiConsole.WriteLine(line);
			}

			AnsiConsole.WriteLine();

			AnsiConsole.WriteException(sdkEx);
		}
	}

	[TypeConverter(typeof(OutputFormatTypeConverter))]
	public enum OutputFormat
	{
		None,
		Json,
		Xml
	}
}