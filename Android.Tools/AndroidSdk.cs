using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Android.Tools
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


		public static FileInfo FindAdb(DirectoryInfo androidHome = null, bool installMissing = true)
			=> FindTool(androidHome, toolName: "adb", ".exe", "platform-tools");

		public static FileInfo FindSdkManager(DirectoryInfo androidHome = null)
			=> FindTool(androidHome, toolName: "sdkmanager", ".bat", "tools", "bin");

		public static FileInfo FindAvdManager(DirectoryInfo androidHome = null, bool installMissing = true)
			=> FindTool(androidHome, toolName: "avdmanager", ".exe", "tools", "bin");

		public static FileInfo FindEmulator(DirectoryInfo androidHome = null, bool installMissing = true)
			=> FindTool(androidHome, toolName: "emulator", "emulator", ".exe");

		static FileInfo FindTool(DirectoryInfo androidHome, string toolName, string windowsExtension, params string[] pathSegments)
		{
			var results = new List<FileInfo>();

			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			var ext = isWindows ? windowsExtension : string.Empty;
			var home = AndroidSdk.FindHome(androidHome)?.FirstOrDefault();

			if (home?.Exists ?? false)
			{
				var allSegments = new List<string>();
				allSegments.Add(home.FullName);
				allSegments.AddRange(pathSegments);
				allSegments.Add(toolName + ext);

				var tool = Path.Combine(allSegments.ToArray());

				if (File.Exists(tool))
					return new FileInfo(tool);
			}

			return null;
		}

		public static void Acquire(params SdkTool[] tools)
		{
			if (tools == null)
				return;

			foreach (var t in tools)
				t.Acquire();
		}
	}
}
