using System.Xml.Linq;

namespace AndroidSdk.Apk;

public class Manifest
{
    public Manifest(XElement element)
    {
		PackageId = element?.Attribute("package")?.Value;

		VersionName = element?.Attribute("versionName")?.Value;

		if (int.TryParse(element?.Attribute("versionCode")?.Value, out var versionCode))
			VersionCode = versionCode;

        var usesSdkElement = element?.Element("uses-sdk");

        UsesSdk = new UsesSdk(usesSdkElement);
	}

    public Manifest(string packageId, string versionName, int versionCode)
    {
		PackageId = packageId;
		VersionName = versionName;
		VersionCode = versionCode;
	}

	public string PackageId { get; set; }

	public string VersionName { get; set; }

	public int VersionCode { get; set; }

    public UsesSdk UsesSdk { get; set; }
}