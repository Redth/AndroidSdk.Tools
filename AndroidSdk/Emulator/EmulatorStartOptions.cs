﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AndroidSdk
{
	public partial class Emulator
	{
		public class EmulatorStartOptions
		{
			public bool NoSnapshotLoad { get; set; }

			public bool NoSnapshotSave { get; set; }

			public bool NoSnapshot { get; set; }

			const string cameraArgumentMessage = "Possible values are `none`, `emulated`, or `webcamX` where `X` is a number.";

			string cameraBack = null;
			public string CameraBack
			{
				get => cameraBack;
				set => cameraBack = ValidateCamera(nameof(CameraBack), value);
			}

			string cameraFront = null;
			public string CameraFront
			{
				get => cameraFront;
				set => cameraFront = ValidateCamera(nameof(CameraFront), value);
			}

			public int? MemoryMegabytes { get; set; }

			public FileInfo SdCard { get; set; }

			public bool WipeData { get; set; }

			public string[] Debug { get; set; }

			public string[] Logcat { get; set; }

			public bool ShowKernel { get; set; }

			public bool Verbose { get; set; }

			public string[] DnsServers { get; set; }

			public string HttpProxy { get; set; }

			public string NetDelay { get; set; }

			public bool NetFast { get; set; }

			public string NetSpeed { get; set; }

			public uint? Port { get; set; }

			public (uint console, uint adb)? Ports { get; set; }

			public FileInfo TcpDump { get; set; }

			public EmulatorAccelerationMode? Acceleration { get; set; }

			public EmulatorEngine? Engine { get; set; }

			public string Gpu { get; set; }

			public bool NoAccel { get; set; }

			public bool NoWindow { get; set; }

			public bool NoJni { get; set; }

			public bool NoAudio { get; set; }

			public EmulatorSeLinux? SeLinux { get; set; }

			public string Timezone { get; set; }

			public bool NoBootAnim { get; set; }

			public EmulatorScreenMode? Screen { get; set; }

			public string[] ExtraArgs { get; set; }

			internal static string ValidateCamera(string paramName, string value)
			{
				if (value is null)
				{
					return null;
				}

				var v = value.ToLowerInvariant();

				if (v != "emulated" && v != "none" && !v.StartsWith("webcam"))
					throw new ArgumentOutOfRangeException(paramName, cameraArgumentMessage);

				if (v.StartsWith("webcam"))
				{
					var n = v.Substring(6);

					if (!int.TryParse(n, out _))
						throw new ArgumentOutOfRangeException(paramName, cameraArgumentMessage);
				}

				return v;
			}
		}

		public enum EmulatorScreenMode
		{
			Touch,
			MultiTouch,
			NoTouch
		}

		public enum EmulatorSeLinux
		{
			Disabled,
			Permissive
		}

		public enum EmulatorAccelerationMode
		{
			Auto,
			Off,
			On
		}

		public enum EmulatorEngine
		{
			Auto,
			Classic,
			Qemu2
		}
	}
}
