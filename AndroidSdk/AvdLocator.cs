#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

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
	}
}
