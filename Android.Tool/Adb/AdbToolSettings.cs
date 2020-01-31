using System;
using System.IO;

namespace Android.Tool.Adb
{
	public class AdbToolSettings
	{
		/// <summary>
		/// Gets or sets the Android SDK HOME root path to invoke tools from.
		/// </summary>
		/// <value>The sdk root.</value>
		public DirectoryInfo AndroidSdkRoot { get; set; }

		/// <summary>
		/// Gets or sets the serial of a specific device or emulator to target commands with.  This must be specified if multiple devices are seen by ADB, and only works on commands that are specific to a device.
		/// </summary>
		/// <value>The serial.</value>
		public string Serial { get; set; }
	}
}
