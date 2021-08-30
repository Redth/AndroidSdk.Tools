using System.IO;
using System.Runtime.InteropServices;

namespace AndroidSdk
{
	public class JdkInfo
	{
		public JdkInfo(string javaCFile, string version)
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			JavaC = new FileInfo(javaCFile);
			Home = new DirectoryInfo(Path.Combine(JavaC.Directory.FullName, ".."));
			Java = new FileInfo(Path.Combine(Home.FullName, "bin", "java" + (isWindows ? ".exe" : "")));
			Version = version;
		}

		public FileInfo JavaC { get; private set; }
		public FileInfo Java { get; private set; }

		public DirectoryInfo Home { get; private set; }

		public string Version { get; set; }
	}
}
