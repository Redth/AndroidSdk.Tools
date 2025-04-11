using System;
using System.IO.Compression;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace AndroidSdk.Apk;

public class AndroidManifest
{
	public AndroidManifest(string apkFile)
	{
		if (string.IsNullOrEmpty(apkFile) || !File.Exists(apkFile))
			throw new FileNotFoundException("APK file not found", apkFile);

		ApkFile = apkFile;

		using var zip = ZipFile.OpenRead(apkFile);
		
		XElement? manifestElement = null;

		foreach (var entry in zip.Entries)
		{
			if (entry.FullName.Equals("AndroidManifest.xml", StringComparison.OrdinalIgnoreCase))
			{
				using var s = entry.Open();
				using var ms = new MemoryStream();

				s.CopyTo(ms);

				var data = ms.ToArray();

				var manifestReader = new AndroidManifestReader(data);
				RawXml = manifestReader.Manifest.ToString();

				manifestElement = manifestReader?.Manifest?.Element("root")?.Element("manifest");
			}
		}

		if (manifestElement == null)
			throw new XmlException("Manifest element at path //root/manifest not found in APK file");

		ManifestElement = manifestElement;
		Manifest = new Manifest(ManifestElement);
	}

	public readonly string ApkFile;

	public readonly string? RawXml;

	public readonly XElement ManifestElement;

	public Manifest Manifest { get; set; }
}
