using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AndroidSdk
{
	public class AndroidSdkManager
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

		public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo specificHome = null)
			=> FindHome(specificHome?.FullName, null);

		public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo specificHome = null, params string[] additionalPossibleDirectories)
			=> FindHome(specificHome?.FullName, additionalPossibleDirectories);

		public static IEnumerable<DirectoryInfo> FindHome(string specificHome = null, params string[] additionalPossibleDirectories)
		{
			var candidates = new List<string>();
			candidates.Add(specificHome);
			candidates.Add(Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"));
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

		public AndroidSdkManager(DirectoryInfo home = null)
		{
			Home = home ?? FindHome()?.FirstOrDefault();

			SdkManager = new SdkManager(Home);
			AvdManager = new AvdManager(Home);
			PackageManager = new PackageManager(Home);
			Adb = new Adb(Home);
			Emulator = new Emulator(Home);
		}

		public async Task Acquire()
		{
			await SdkManager.Acquire();
		}

		public readonly DirectoryInfo Home;

		public readonly SdkManager SdkManager;

		public readonly AvdManager AvdManager;

		public readonly PackageManager PackageManager;

		public readonly Emulator Emulator;

		public readonly Adb Adb;
	}
}
