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
		[Obsolete("Use AndroidSdkLocator.Locate instead.")]
		public static IEnumerable<DirectoryInfo> FindHome()
			=> new SdkLocator().Locate();

		[Obsolete("Use AndroidSdkLocator.Locate instead.")]
		public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo specificHome = null)
			=> new SdkLocator().Locate(specificHome?.FullName, null);

		[Obsolete("Use AndroidSdkLocator.Locate instead.")]
		public static IEnumerable<DirectoryInfo> FindHome(DirectoryInfo specificHome = null, params string[] additionalPossibleDirectories)
			=> new SdkLocator().Locate(specificHome?.FullName, additionalPossibleDirectories);

		[Obsolete("Use AndroidSdkLocator.Locate instead.")]
		public static IEnumerable<DirectoryInfo> FindHome(string specificHome = null, params string[] additionalPossibleDirectories)
			=> new SdkLocator().Locate(specificHome, additionalPossibleDirectories);

		public AndroidSdkManager(DirectoryInfo home = null)
		{
			Home = new SdkLocator().Locate(home?.FullName)?.FirstOrDefault();

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
