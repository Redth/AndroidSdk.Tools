using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Android.Tool
{
	public static class AndroidSdk
	{
		static string[] KnownLikelyPaths =>
			RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
				new string[] {
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk"),
				} :
				new string []
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "android-sdk-macosx"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "Xamarin", "android-sdk-macosx"),
					Path.Combine("Developer", "Android", "android-sdk-macosx"),
				};

		public static IEnumerable<DirectoryInfo> FindHome()
			=> FindHome((string)null, null);

		public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo mostLikelyHome = null)
			=> FindHome(mostLikelyHome?.FullName, null);

		public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo mostLikelyHome = null, params string[] additionalPossibleDirectories)
			=> FindHome(mostLikelyHome?.FullName, additionalPossibleDirectories);

		public static IEnumerable<DirectoryInfo> FindHome(string mostLikelyHome = null, params string[] additionalPossibleDirectories)
		{
			var candidates = new List<string>();
			candidates.Add(mostLikelyHome);
			candidates.Add(Environment.GetEnvironmentVariable("ANDROID_HOME"));
			if (additionalPossibleDirectories != null)
				candidates.AddRange(additionalPossibleDirectories);
			candidates.AddRange(KnownLikelyPaths);

			foreach (var c in candidates)
			{
				if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c))
					yield return new DirectoryInfo(c);
			}
		}


		public static FileInfo FindAdb(DirectoryInfo androidHome = null)
		{
			var results = new List<FileInfo>();

			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			var ext = isWindows ? ".exe" : "";
			var home = FindHome(androidHome)?.FirstOrDefault();

			if (home?.Exists ?? false)
			{
				var adb = Path.Combine(home.FullName, "platform-tools", "adb" + ext);
				if (File.Exists(adb))
					return new FileInfo(adb);
			}

			return null;
		}

		public static FileInfo FindSdkManager(DirectoryInfo androidHome = null)
		{
			var results = new List<FileInfo>();

			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			var ext = isWindows ? ".exe" : "";
			var home = AndroidSdk.FindHome(androidHome)?.FirstOrDefault();

			if (home?.Exists ?? false)
			{
				var sdkManager = Path.Combine(home.FullName, "tools", "bin", "sdkmanager" + ext);

				if (File.Exists(sdkManager))
					return new FileInfo(sdkManager);
			}

			return null;
		}
	}
}
