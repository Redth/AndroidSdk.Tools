#nullable enable
using System.IO;

namespace AndroidSdk
{
	public class SdkToolOptions
	{
		public SdkToolOptions()
		{
		}

		public SdkToolOptions(string? androidSdkHome)
			: this(string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome))
		{
		}

		public SdkToolOptions(DirectoryInfo? androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
		}

		public DirectoryInfo? AndroidSdkHome { get; set; }
	}
}
