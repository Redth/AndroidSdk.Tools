using System.IO;

namespace AndroidSdk;

public class SdkManagerToolOptions : SdkToolOptions
{
	public SdkManagerToolOptions()
		: base()
	{
	}

	public SdkManagerToolOptions(string? androidSdkHome)
		: base(androidSdkHome)
	{
	}

	public SdkManagerToolOptions(DirectoryInfo? androidSdkHome)
		: base(androidSdkHome)
	{
	}


	public SdkManager.SdkChannel Channel { get; set; } = SdkManager.SdkChannel.Stable;

	public bool SkipVersionCheck { get; set; } = false;

	public bool IncludeObsolete { get; set; } = false;

	public SdkManager.SdkManagerProxyOptions? Proxy { get; set; } = null;
}
