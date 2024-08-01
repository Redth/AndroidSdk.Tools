using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidSdk.Tool
{
	public class AvdStartCommandSettings : CommandSettings
	{
		[Description("Name of the AVD/Emulator")]
		[CommandOption("-n|--name")]
		public string Name { get; set; }

		[Description("Wait for emulator to boot")]
		[CommandOption("-w|--wait|--wait-boot")]
		[DefaultValue(false)]
		public bool WaitForBoot { get; set; }

		[Description("Wait for emulator process to exit")]
		[CommandOption("--wait-exit")]
		[DefaultValue(false)]
		public bool WaitForExit { get; set; }

		[Description("Timeout in seconds if waiting for emulator to boot")]
		[CommandOption("-t|--timeout")]
		public int? Timeout { get; set; }

		[Description("Wipe data")]
		[CommandOption("--wipe|--wipe-data")]
		[DefaultValue(false)]
		public bool WipeData { get; set; }

		[Description("Android SDK home/root path")]
		[CommandOption("-h|--home")]
		public string Home { get; set; }

		[Description("Disable Snapshot load/save")]
		[CommandOption("--no-snapshot")]
		[DefaultValue(false)]
		public bool NoSnapshot { get; set; }

		[Description("Disable Snapshot load")]
		[CommandOption("--no-snapshot-load")]
		[DefaultValue(false)]
		public bool NoSnapshotLoad { get; set; }

		[Description("Disable Snapshot save")]
		[CommandOption("--no-snapshot-save")]
		[DefaultValue(false)]
		public bool NoSnapshotSave { get; set; }

		[Description("Emulation mode for a camera facing backwards (Possible values: emulated, webcam#, none)")]
		[CommandOption("--camera-back")]
		[TypeConverter(typeof(EmulatorCameraTypeConverter))]
		public string? CameraBack { get; set; }

		[Description("Emulation mode for a camera facing forward (Possible values: emulated, webcam#, none)")]
		[CommandOption("--camera-front")]
		[TypeConverter(typeof(EmulatorCameraTypeConverter))]
		public string? CameraFront { get; set; }

		[Description("The TCP port number for the console and adb (Possible values: ranges from 5554 to 5682)")]
		[CommandOption("-p|--port")]
		public uint? Port { get; set; }

		[Description("The physical RAM size in MB (Possible values: ranges from 1536 to 8192)")]
		[CommandOption("--memory")]
		public uint? Memory { get; set; }

		[Description("The system/data partition size in MB")]
		[CommandOption("--partition-size|--data-partition-size")]
		public uint? PartitionSize { get; set; }

		[Description("The cache partition size in MB (Default is 66 MB)")]
		[CommandOption("--cache-size|--cache-partition-size")]
		public uint? CacheSize { get; set; }

		[Description("Emulated touch screen mode (Possible values: touch, multi-touch, no-touch)")]
		[CommandOption("--screen")]
		[TypeConverter(typeof(EmulatorScreenModeTypeConverter))]
		public Emulator.EmulatorScreenMode? ScreenMode { get; set; }

		[Description("Emulator engine (Possible values: auto, classic, qemu2)")]
		[CommandOption("--engine")]
		public Emulator.EmulatorEngine? Engine { get; set; }

		[Description("Acceleration mode (Possible values: auto, off, on)")]
		[CommandOption("--accel|--acceleration")]
		public Emulator.EmulatorAccelerationMode? Acceleration { get; set; }

		[Description("Disable boot animation during emulator startup for faster booting")]
		[CommandOption("--no-boot-anim|--no-boot-animation")]
		[DefaultValue(false)]
		public bool NoBootAnimation { get; set; }

		[Description("Disable graphical window display")]
		[CommandOption("--no-window")]
		[DefaultValue(false)]
		public bool NoWindow { get; set; }

		[Description("Disable audio support")]
		[CommandOption("--no-audio")]
		[DefaultValue(false)]
		public bool NoAudio { get; set; }

		[Description("GPU emulation mode")]
		[CommandOption("--gpu")]
		public string Gpu { get; set; }

		[Description("Disable extended Java Native Interface (JNI) checks")]
		[CommandOption("--no-jni")]
		[DefaultValue(false)]
		public bool NoJni { get; set; }

		[Description("Print emulator initialization messages")]
		[CommandOption("-v|--verbose")]
		[DefaultValue(false)]
		public bool Verbose { get; set; }

		public override ValidationResult Validate()
		{
			if (string.IsNullOrEmpty(Name))
				return ValidationResult.Error("Missing --name");

			return ValidationResult.Success();
		}

		class EmulatorScreenModeTypeConverter : TypeConverter
		{
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if (value is string stringValue)
				{
					stringValue = stringValue.Replace("-", "");
					if (Enum.TryParse<Emulator.EmulatorScreenMode>(stringValue, true, out var mode))
						return mode;
				}
				throw new ArgumentOutOfRangeException("Can't convert value to emulator screen mode.");
			}
		}

		class EmulatorCameraTypeConverter : TypeConverter
		{
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				return Emulator.EmulatorStartOptions.ValidateCamera(nameof(ScreenMode), value?.ToString());
			}
		}
	}

	public class AvdStartCommand : Command<AvdStartCommandSettings>
	{
		public override int Execute([NotNull] CommandContext context, [NotNull] AvdStartCommandSettings settings)
		{
			var ok = true;

			try
			{
				var emu = new Emulator(settings?.Home);

				Emulator.AndroidEmulatorProcess process = null;

				AnsiConsole.Status()
				.Start($"Starting {settings.Name}...", ctx =>
				{
					process = emu.Start(settings.Name, new Emulator.EmulatorStartOptions
					{
						WipeData = settings.WipeData,
						NoSnapshot = settings.NoSnapshot,
						Acceleration = settings.Acceleration,
						Engine = settings.Engine,
						NoWindow = settings.NoWindow,
						NoAudio = settings.NoAudio,
						NoJni = settings.NoJni,
						NoBootAnim = settings.NoBootAnimation,
						Gpu = settings.Gpu,
						Port = settings.Port,
						CameraBack = settings.CameraBack,
						CameraFront = settings.CameraFront,
						NoSnapshotLoad = settings.NoSnapshotLoad,
						NoSnapshotSave = settings.NoSnapshotSave,
						Verbose = settings.Verbose,
						Screen = settings.ScreenMode,
						MemoryMegabytes = (int?)settings.Memory,
						PartitionSizeMegabytes = (int?)settings.PartitionSize,
						CacheSizeMegabytes = (int?)settings.CacheSize,
					});

					var timeout = settings.Timeout.HasValue ? TimeSpan.FromSeconds(settings.Timeout.Value) : TimeSpan.Zero;

					if (settings.WaitForBoot)
					{
						ctx.Status($"Waiting for {settings.Name} to finish booting...");
						ok = process.WaitForBootComplete(timeout);
					}

					if (settings.WaitForExit)
					{
						ctx.Status($"Booted, waiting for {settings.Name} to exit...");
						ok = process.WaitForExit() == 0;
					}
				});

				if (!ok)
				{
					AnsiConsole.WriteException(new Exception("Failed to start AVD." + Environment.NewLine + string.Join(Environment.NewLine, process.GetOutput())));
				}

			}
			catch (SdkToolFailedExitException sdkEx)
			{
				Program.WriteException(sdkEx);
				return 1;
			}

			return ok ? 0 : 1;
		}
	}
}
