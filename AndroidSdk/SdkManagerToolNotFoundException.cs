#nullable enable
using System;
using System.IO;

namespace AndroidSdk;

/// <summary>
/// Exception thrown when the sdkmanager tool (from cmdline-tools) cannot be found in the Android SDK.
/// </summary>
public class SdkManagerToolNotFoundException : InvalidOperationException
{
	const string DefaultMessage = "The Android SDK at '{0}' is missing 'cmdline-tools' (which provides sdkmanager and avdmanager). " +
		"Install cmdline-tools via Android Studio's SDK Manager, or run: android sdk download --home \"{0}\"";

	const string NoSdkMessage = "Could not locate the Android SDK. Set the ANDROID_HOME environment variable or use --home to specify the SDK path.";

	public SdkManagerToolNotFoundException(DirectoryInfo? sdkHome)
		: base(sdkHome is not null ? string.Format(DefaultMessage, sdkHome.FullName) : NoSdkMessage)
	{
		SdkHome = sdkHome;
	}

	/// <summary>
	/// The SDK home directory that was searched (may be null if no SDK was found).
	/// </summary>
	public DirectoryInfo? SdkHome { get; }
}
