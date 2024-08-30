
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

public class MonoDroidSdkLocation
{
	public MonoDroidSdkLocation(string? androidSdkPath = null, string? javaJdkPath = null)
	{
		AndroidSdkPath = androidSdkPath;
		JavaJdkPath = javaJdkPath;
	}

	public readonly string? AndroidSdkPath;
	public readonly string? JavaJdkPath;
}

public static class MonoDroidSdkLocator
{
	internal static bool IsWindows
		=> RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

	/*
	<?xml version="1.0" encoding="utf-8"?>
	<monodroid>
	<android-sdk path="/Users/redth/Library/Developer/Xamarin/android-sdk-macosx" />
	<java-sdk path="/Library/Java/JavaVirtualMachines/microsoft-11.jdk/Contents/Home" />
	</monodroid>%  
	*/
	public static MonoDroidSdkLocation ReadConfigFile()
	{
		// Load the XML file
		var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "xbuild", "monodroid-config.xml");
		if (File.Exists(path))
		{
			var doc = new System.Xml.XmlDocument();
			doc.Load(path);
			
			return new MonoDroidSdkLocation(
				doc.SelectSingleNode("//monodroid/android-sdk")?.Attributes?["path"]?.Value,
				doc.SelectSingleNode("//monodroid/java-sdk")?.Attributes?["path"]?.Value);
		}

		return new MonoDroidSdkLocation();;
	}

	public static void WriteConfigFile(MonoDroidSdkLocation location)
	{
		// Load the XML file
		var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "xbuild", "monodroid-config.xml");
		if (File.Exists(path))
		{
			var doc = new System.Xml.XmlDocument();
			doc.Load(path);

			var monodroidNode = doc.SelectSingleNode("//monodroid");
			if (monodroidNode == null)
			{
				monodroidNode = doc.CreateElement("monodroid");
				doc.AppendChild(monodroidNode);
			}
			
			if (!string.IsNullOrEmpty(location.AndroidSdkPath))
			{
				var androidSdkNode = monodroidNode.SelectSingleNode("//monodroid/android-sdk");
				if (androidSdkNode == null)
				{
					androidSdkNode = doc.CreateElement("android-sdk");
					monodroidNode.AppendChild(androidSdkNode);
				}
				if (androidSdkNode.Attributes["path"] == null)
					androidSdkNode.Attributes.Append(doc.CreateAttribute("path"));
				androidSdkNode.Attributes["path"].Value = location.AndroidSdkPath;
			}
			
			if (!string.IsNullOrEmpty(location.JavaJdkPath))
			{
				var javaSdkNode = doc.SelectSingleNode("//monodroid/java-sdk");
				if (javaSdkNode == null)
				{
					javaSdkNode = doc.CreateElement("java-sdk");
					monodroidNode.AppendChild(javaSdkNode);
				}
				if (javaSdkNode.Attributes["path"] == null)
					javaSdkNode.Attributes.Append(doc.CreateAttribute("path"));
				javaSdkNode.Attributes["path"].Value = location.JavaJdkPath;
			}

			doc.Save(path);
		}
	}

	public static MonoDroidSdkLocation ReadRegistry()
	{
		if (!IsWindows)
			return new MonoDroidSdkLocation();

		// Define the registry key path
		string[] registryPaths = [ 
			"SOFTWARE\\Novell\\Mono for Android",
			"SOFTWARE\\Xamarin\\MonoAndroid"
		];

		string? androidSdkPath = null;
		string? javaJdkPath = null;

		foreach (var registryPath in registryPaths)
		{
			// Open the registry key under HKCU (HKEY_CURRENT_USER)
			using var key = Registry.CurrentUser.OpenSubKey(registryPath);
			
			// Only set if we didn't get one yet
			if (string.IsNullOrEmpty(androidSdkPath))
				androidSdkPath = key?.GetValue("AndroidSdkDirectory") as string;
			if (string.IsNullOrEmpty(javaJdkPath))
				javaJdkPath = key?.GetValue("JavaSdkDirectory") as string;
		}
		
		return new MonoDroidSdkLocation(androidSdkPath, javaJdkPath);
	}

	public static void WriteRegistry(MonoDroidSdkLocation location)
	{
		if (!IsWindows)
			return;

		if (string.IsNullOrEmpty(location.AndroidSdkPath) && string.IsNullOrEmpty(location.JavaJdkPath))
			return;

		// Define the registry key path
		string[] registryPaths = [ 
			"SOFTWARE\\Novell\\Mono for Android",
			"SOFTWARE\\Xamarin\\MonoAndroid"
		];

		foreach (var registryPath in registryPaths)
		{
			// Open or create the registry key under HKCU (HKEY_CURRENT_USER)
			using var key = Registry.CurrentUser.OpenSubKey(registryPath)
				?? Registry.CurrentUser.CreateSubKey(registryPath);

			// Only set if we didn't get one yet
			if (!string.IsNullOrEmpty(location.AndroidSdkPath))
				key?.SetValue("AndroidSdkDirectory", location.AndroidSdkPath);
			if (!string.IsNullOrEmpty(location.JavaJdkPath))
				key?.SetValue("JavaSdkDirectory", location.JavaJdkPath);
		}
	}
}