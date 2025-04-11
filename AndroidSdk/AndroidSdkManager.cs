using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AndroidSdk
{
	public class AndroidSdkManager
	{
		public AndroidSdkManager(DirectoryInfo? androidSdkHome = null, DirectoryInfo? jdkHome = null, SdkManagerToolOptions? options = null)
		{
			Home = new SdkLocator().Locate(androidSdkHome?.FullName)?.FirstOrDefault()
				?? throw new DirectoryNotFoundException("Unable to find Android SDK");

			JdkHome = new JdkLocator().Locate(jdkHome?.FullName)?.FirstOrDefault()
				?? throw new DirectoryNotFoundException("Unable to find Java JDK");
		}

		public readonly DirectoryInfo Home;

		public readonly DirectoryInfo JdkHome;

		SdkManager? sdkManager;
		AvdManager? avdManager;
		PackageManager? packageManager;
		Emulator? emulator;
		Adb? adb;

		public SdkManager SdkManager => sdkManager ??= new SdkManager(Home, JdkHome);

		public AvdManager AvdManager => avdManager ??= new AvdManager(Home, JdkHome);

		public PackageManager PackageManager => packageManager ??= new PackageManager(Home, JdkHome);

		public Emulator Emulator => emulator ??= new Emulator(Home);
		
		public Adb Adb => adb ??= new Adb(Home);
	}
}
