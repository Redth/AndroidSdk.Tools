#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace AndroidSdk
{

	public class AvdLocator : PathLocator
	{
		public override string[] PreferredPaths()
			=> new[]
			{
				Environment.GetEnvironmentVariable("ANDROID_AVD_ROOT"),
				Environment.GetEnvironmentVariable("ANDROID_AVD_HOME"),
				Path.Combine(Environment.GetEnvironmentVariable("ANDROID_PREFS_ROOT"), ".android"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "avd")
			};
	}
}
