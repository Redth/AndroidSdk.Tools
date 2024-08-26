#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AndroidSdk;

public class SdkLocator : PathLocator
{
	public override string[] PreferredPaths()
		=> new[]
		{
			Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
			Environment.GetEnvironmentVariable("ANDROID_HOME")
		};

	public override string[] AdditionalPaths()
		=> RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
			new [] {
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "android-sdk"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android"),
			} :
			new []
			{
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "android-sdk-macosx"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "Xamarin", "android-sdk-macosx"),
				Path.Combine("Developer", "Android", "android-sdk-macosx"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Android", "sdk"),
			};
}
