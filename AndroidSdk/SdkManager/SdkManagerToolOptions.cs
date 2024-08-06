#nullable enable

namespace AndroidSdk;

public class SdkManagerToolOptions : SdkToolOptions
{
	public SdkManager.SdkChannel Channel { get; set; } = SdkManager.SdkChannel.Stable;

	public bool SkipVersionCheck { get; set; } = false;

	public bool IncludeObsolete { get; set; } = false;

	public SdkManager.SdkManagerProxyOptions? Proxy { get; set; } = null;
}
