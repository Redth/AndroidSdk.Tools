#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AndroidSdk
{
	public class AvdLocator : PathLocator
	{
		public override string[] PreferredPaths()
		{
			var paths = new List<string>
			{
				Environment.GetEnvironmentVariable("ANDROID_AVD_ROOT"),
				Environment.GetEnvironmentVariable("ANDROID_AVD_HOME"),
			};

			var prefsRoot = Environment.GetEnvironmentVariable("ANDROID_PREFS_ROOT");
			if (IsValidDirectoryPath(prefsRoot))
				paths.Add(Path.Combine(prefsRoot, ".android"));

			paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "avd"));

			return paths.ToArray();
		}


		public IReadOnlyList<AvdInfo> ListAvds(string? specificHome)
		{
			var files = new List<AvdInfo>();
			
			var home = Locate(specificHome)?.FirstOrDefault();
			if (home is null)
				return files;

			foreach (var iniFile in home.EnumerateFiles("*.ini", SearchOption.TopDirectoryOnly))
			{
				// File:        Pixel_5_API_31.ini
				// AVD Name:    Pixel_5_API_31
				var avdName = Path.GetFileNameWithoutExtension(iniFile.Name);

				// AVD Dir:     Pixel_5_API_31.avd
				var avdDir = new DirectoryInfo(Path.Combine(iniFile.Directory.FullName, $"{avdName}.avd"));
				// AVD Config:  Pixel_5_API_31.avd/config.ini
				var avdConfigIni = new FileInfo(Path.Combine(avdDir.FullName, "config.ini"));

				if (avdDir.Exists && avdConfigIni.Exists)
					files.Add(new AvdInfo(avdName, avdDir, iniFile, avdConfigIni));
			}

			return files;
		}
	}

	public class AvdInfo
	{
		public AvdInfo(string name, DirectoryInfo directory, FileInfo ini, FileInfo configIni, bool skipParsing = false)
		{
			Name = name;
			Directory = directory;
			Ini = ini;
			ConfigIni = configIni;

			if (!skipParsing)
				LoadProperties();
		}

		public readonly string Name;

		public readonly FileInfo Ini;
		public readonly FileInfo ConfigIni;
		public readonly DirectoryInfo Directory;

		Dictionary<string, string> props = new();

		public string? DeviceName
			=> TryGetProp("hw.device.name");

		public string? Target
			=> TryGetProp("target");

		public string? DisplayName
			=> TryGetProp("avd.ini.displayname", Name);
		public string? Path
			=> TryGetProp("path", Directory.FullName);

		public string? ApiLevel
		{
			get
			{
				// Target is the format `platform-#` where # is the api level (ie: 21, 22, 30, 31, 32, 33)
				var targetLastPart = Target?.Split(new[] { '-' }, 2, StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault();

				if (!string.IsNullOrEmpty(targetLastPart) && int.TryParse(targetLastPart, out var v) && v >= 0)
					return v.ToString();
				
				return null;
			}
		}

		public string? TryGetProp(string name, string? defaultValue = null)
		{
			if (props.TryGetValue(name, out var v))
				return v;
			return defaultValue;
		}

		public IReadOnlyDictionary<string, string> Properties
			=> props;

		public void LoadProperties()
		{
			try { ParseIniFile(props, Ini.FullName); }
			catch { }

			try { ParseIniFile(props, ConfigIni.FullName); }
			catch { }
		}

		static void ParseIniFile(Dictionary<string, string> properties, string iniFile)
		{
			if (!File.Exists(iniFile))
				return;

			var lines = File.ReadAllLines(iniFile);
			foreach (var line in lines)
			{
				if (!line.Contains('='))
					continue;

				var parts = line.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

				if (parts is not null && parts.Length == 2)
				{
					var key = parts[0]?.Trim()?.ToLowerInvariant();

					if (key is not null && !string.IsNullOrEmpty(key))
						properties[key] = parts[1].Trim();
				}
			}
		}
	}
}
