using System;
using System.Collections.Generic;
using System.Text;

namespace Android.Tool.Adb
{
	public enum AdbTransport
	{
		Any = 0,
		Usb = 1,
		Local = 2
	}

	public enum AdbState
	{
		Device = 0,
		Recovery,
		Sideload,
		Bootloader
	}

	/// <summary>
	/// Enabled/Disabled state of packages.
	/// </summary>
	public enum PackageListState
	{
		/// <summary>
		/// All - Enabled and Disabled.
		/// </summary>
		All,
		/// <summary>
		/// Only enabled.
		/// </summary>
		OnlyEnabled,
		/// <summary>
		/// Only Disabled.
		/// </summary>
		OnlyDisabled
	}

	/// <summary>
	/// Source type of packages
	/// </summary>
	public enum PackageSourceType
	{
		/// <summary>
		/// All - System and Third Party.
		/// </summary>
		All,
		/// <summary>
		/// Only System.
		/// </summary>
		OnlySystem,
		/// <summary>
		/// Only Third Party.
		/// </summary>
		OnlyThirdParty
	}

	/// <summary>
	/// Install Location of packages.
	/// </summary>
	public enum AdbInstallLocation
	{
		/// <summary>
		/// Auto - Let the system automatically decide.
		/// </summary>
		Auto = 0,
		/// <summary>
		/// Internal - System Memory.
		/// </summary>
		Internal = 1,
		/// <summary>
		/// External - Mass Storage Device.
		/// </summary>
		External = 2
	}
}
