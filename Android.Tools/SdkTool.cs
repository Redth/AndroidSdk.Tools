using System.IO;

namespace Android.Tools
{
	public abstract class SdkTool
	{
		public SdkTool()
		{ }

		public SdkTool(string androidSdkHome)
			: this(new DirectoryInfo(androidSdkHome))
		{ }

		public SdkTool(DirectoryInfo androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
		}

		internal abstract string SdkPackageId { get; }

		public DirectoryInfo AndroidSdkHome { get; internal set; }

		public void Acquire()
		{
			var sdkManager = new SdkManager(AndroidSdkHome);

			sdkManager.Acquire(SdkPackageId);
		}
	}
}
