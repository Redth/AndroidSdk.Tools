using System.Xml.Linq;

namespace AndroidSdk.Apk;

public class UsesSdk
{
	public UsesSdk(XElement element)
	{
		if (int.TryParse(element?.Attribute("minSdkVersion")?.Value, out var minSdkVersion))
			MinSdkVersion = minSdkVersion;

		if (int.TryParse(element?.Attribute("targetSdkVersion")?.Value, out var targetSdkVersion))
			TargetSdkVersion = targetSdkVersion;

		if (int.TryParse(element?.Attribute("maxSdkVersion")?.Value, out var maxSdkVersion))
			MaxSdkVersion = maxSdkVersion;
	}

	public readonly int MinSdkVersion;
	public readonly int TargetSdkVersion;
	public readonly int MaxSdkVersion;
}