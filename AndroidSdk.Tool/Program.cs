using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.DragonFruit;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidSdk.Tool
{
	class Program
	{
		public static void Main(string[] args)
		{
			var homeOption = new Option(new string[] { "--home", "-h" }, "Location of Android SDK Home") { Argument = new Argument<string>("home", () => null) { Arity = ArgumentArity.ZeroOrOne } };
			var adbSerialOption = new Option(new string[] { "--serial", "-s" }, "ADB Device Serial") { Argument = new Argument<string>("serial", () => null) };
			var outputFormatOption = new Option(new string[] { "--format", "-f" }, "Output Format") { Argument = new Argument<OutputFormat>("format", () => OutputFormat.None) };

			var sdkListCommand = new Command("list", "Lists SDK Manager Packages")
			{
				homeOption,
				outputFormatOption,
				new Option(new string[] { "--available", "-a" }, "Shows Available SDK Manager Packages"),
				new Option(new string[] { "--installed", "-i" }, "Shows Installed Packages")
			};
			sdkListCommand.Handler = CommandHandler.Create<string, OutputFormat, bool, bool>((home, format, available, installed) =>
			{
				var sdk = GetSdk(home);

				var list = sdk.SdkManager.List();

				if (!available)
					list.AvailablePackages.Clear();
				if (!installed)
					list.InstalledPackages.Clear();

				OutputHelper.Output(list, format);
			});

			var sdkDownloadCommand = new Command("download", "Downloads and updates the Android SDK")
			{
				homeOption
			};
			sdkDownloadCommand.Handler = CommandHandler.Create<string>((home) =>
			{
				var sdk = GetSdk(home);
				sdk.Acquire();
				sdk.SdkManager.UpdateAll();
			});

			var sdkCommand = new Command("sdk", "List, Install, Update or Remove SDK Manager Packages")
			{
				homeOption,
				outputFormatOption,
				new Option(new string[] { "--install", "-i", "--update", "-u" }, "Install or Update SDK Manager Package") {
					Argument = new Argument<string[]>("install") { Arity = ArgumentArity.ZeroOrMore }
				},
				new Option(new string[] { "--delete", "-d"}, "Remove SDK Manager Package")
				{
					Argument = new Argument<string[]>("uninstall") { Arity = ArgumentArity.ZeroOrMore }
				},
				sdkListCommand,
				sdkDownloadCommand
			};
			sdkCommand.Handler = CommandHandler.Create<string, string[], string[]>((home, install, uninstall) =>
			{
				var sdk = GetSdk(home);

				if (install?.Any() ?? false)
					sdk.SdkManager.Install(install);

				if (uninstall?.Any() ?? false)
					sdk.SdkManager.Uninstall(uninstall);

				if ((uninstall?.Any() ?? false) && (install?.Any() ?? false))
					sdk.SdkManager.UpdateAll();
			});

			var deviceListCommand = new Command("list", "Lists all devices from ADB")
			{
				homeOption,
				outputFormatOption,
				new Option(new string[] { "--property", "-p" }, "Device/Emulator property to return, if none specified, all are returned")
				{
					Argument = new Argument<string[]>("property") { Arity = ArgumentArity.ZeroOrMore }
				},
				new Option(new string[] { "--id", "-i" }, "Filter to a specific device id")
				{
					Argument = new Argument<string>("id")
				},
			};
			deviceListCommand.Handler = CommandHandler.Create<string, OutputFormat, string[], string>((home, format, property, id) =>
			{
				var sdk = GetSdk(home);

				var adbDevices = sdk.Adb.GetDevices();

				foreach (var device in adbDevices)
				{
					if (string.IsNullOrEmpty(id) || device.Serial.Equals(id, StringComparison.OrdinalIgnoreCase))
					{
						var p = sdk.Adb.GetProperties(device.Serial, property);

						var printSerial = string.IsNullOrWhiteSpace(id);

						if (adbDevices.Count <= 1)
							printSerial = false;

						var d = new DeviceInfo(device, p, printSerial);

						OutputHelper.Output(d, format);
					}
				}
			});

			var deviceCommand = new Command("device", "Interface with ADB Devices")
			{
				homeOption,
				outputFormatOption,
				deviceListCommand
			};

			var emulatorListDevicesCommand = new Command("devices", "List available emulator device types")
			{
				homeOption,
				outputFormatOption,
			};
			emulatorListDevicesCommand.Handler = CommandHandler.Create<string, OutputFormat>((home, format) =>
			{
				var sdk = GetSdk(home);
				var devices = sdk.AvdManager.ListDevices();
				OutputHelper.Output(devices, format);
			});

			var emulatorListTargetsCommand = new Command("targets", "List available emulator device types")
			{
				homeOption,
				outputFormatOption,
			};
			emulatorListTargetsCommand.Handler = CommandHandler.Create<string, OutputFormat>((home, format) =>
			{
				var sdk = GetSdk(home);
				var targets = sdk.AvdManager.ListTargets();
				OutputHelper.Output(targets, format);
			});

			var emulatorListCommand = new Command("list", "List available emulator instances")
			{
				homeOption,
				outputFormatOption,
				emulatorListDevicesCommand,
				emulatorListTargetsCommand,
			};
			emulatorListCommand.Handler = CommandHandler.Create<string, OutputFormat>((home, format) =>
			{
				var sdk = GetSdk(home);
				var avds = sdk.AvdManager.ListAvds();
				OutputHelper.Output(avds, format);
			});

			var emulatorCreateCommand = new Command("create", "Create an Emulator AVD")
			{
				homeOption,
				outputFormatOption,
				new Argument<string>("name") { Description = "Name of AVD to create", Arity = ArgumentArity.ExactlyOne },
				new Argument<string>("sdk") { Description = "SDK Manager System Image Package ID", Arity = ArgumentArity.ExactlyOne },
				new Option(new string[] { "--device", "-d" }, "Optional Device ID") { Argument = new Argument<string>("device") { Arity = ArgumentArity.ZeroOrOne }},
				new Option(new string[] { "--force" }, "Force creation"),
				new Option(new string[] { "--path", "-p" }, "Path to create AVD in") { Argument = new Argument<string>("path") { Arity = ArgumentArity.ZeroOrOne }}
			};
			emulatorCreateCommand.Handler = CommandHandler.Create<string, OutputFormat, string, string, string, bool, string>((home, format, name, sdk, device, force, path) =>
			{
				var s = GetSdk(home);

				s.AvdManager.Create(name, sdk, device, path, force);				
			});

			var emulatorDeleteCommand = new Command("delete", "Create an Emulator AVD")
			{
				homeOption,
				outputFormatOption,
				new Argument<string>("name") { Description = "Name of AVD to delete", Arity = ArgumentArity.ExactlyOne },
			};
			emulatorDeleteCommand.Handler = CommandHandler.Create<string, OutputFormat, string>((home, format, name) =>
			{
				var s = GetSdk(home);

				s.AvdManager.Delete(name);
			});

			var emulatorRunCommand = new Command("start", "Starts an Emulator AVD")
			{
				homeOption,
				outputFormatOption,
				new Argument<string>("name") { Description = "Name of AVD to run", Arity = ArgumentArity.ExactlyOne },
				new Option(new string[] { "--wait", "-w" }, "Wait for the emulator to be booted")
			};
			emulatorRunCommand.Handler = CommandHandler.Create<string, OutputFormat, string, bool>((home, format, name, wait) =>
			{
				var s = GetSdk(home);

				var e = s.Emulator.Start(name);

				if (wait)
				{
					var booted = e.WaitForBootComplete();
					if (!booted)
						Console.Error.WriteLine($"Emulator Start Failed: {name}");
				}
			});

			var emulatorCommand = new Command("emulator", "List, Create, Run, Delete (AVD's) Android Emulators")
			{
				homeOption,
				outputFormatOption,
				emulatorListCommand,
				emulatorCreateCommand,
				emulatorDeleteCommand,
				emulatorRunCommand
			};

			var rc = new RootCommand
			{
				homeOption,
				outputFormatOption,
				sdkCommand,
				deviceCommand,
				emulatorCommand
			};

			try
			{
				rc.Invoke(args);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
			}
		}

		static AndroidSdkManager GetSdk(string home)
		{
			DirectoryInfo h = null;

			if (!string.IsNullOrEmpty(home))
				h = new DirectoryInfo(home);

			return new AndroidSdkManager(h);
		}
	}

	public enum OutputFormat
	{
		None,
		Json,
		Xml
	}
}