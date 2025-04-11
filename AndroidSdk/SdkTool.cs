#nullable enable
using System.IO;

namespace AndroidSdk
{
	public abstract class JdkTool(DirectoryInfo androidSdkHome, DirectoryInfo jdkHome) : SdkTool(androidSdkHome)
	{
		public DirectoryInfo JdkHome => jdkHome;
	}

	public abstract class SdkTool(DirectoryInfo androidSdkHome)
	{
		public DirectoryInfo AndroidSdkHome => androidSdkHome;
	}
}
