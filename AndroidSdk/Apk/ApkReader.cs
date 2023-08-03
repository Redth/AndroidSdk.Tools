using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AndroidSdk.Apk;

public class ApkReader
{
	public ApkReader(string apkFile)
	{
		if (string.IsNullOrEmpty(apkFile) || !File.Exists(apkFile))
			throw new FileNotFoundException("APK file not found", apkFile);

		ApkFile = apkFile;
	}


	public AndroidManifest ReadManifest()
	{
		return new AndroidManifest(ApkFile);
	}

	public readonly string ApkFile;
}
