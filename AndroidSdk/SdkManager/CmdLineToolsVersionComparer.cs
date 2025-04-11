#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace AndroidSdk;

public partial class SdkManager
{
	internal static bool TryParseCmdlineToolsVersion(DirectoryInfo? dir, out Version? version)
	{
		if (dir is null)
		{
			version = null;
			return false;
		}

		// first look for the version in the source.properties file
		var sourceProperties = new FileInfo(Path.Combine(dir.FullName, "source.properties"));
		if (sourceProperties.Exists)
		{
			using var stream = sourceProperties.OpenRead();
			using var reader = new StreamReader(stream);
			string? line;
			while ((line = reader.ReadLine()) is not null)
			{
				if (line.StartsWith("Pkg.Revision=", StringComparison.OrdinalIgnoreCase))
				{
					var revision = line.Substring("Pkg.Revision=".Length).Trim();
					if (Version.TryParse(revision, out var v))
					{
						version = v;
						return true;
					}
				}
			}
		}

		// if we didn't find the version in the source.properties file, use the directory name
		return Version.TryParse(dir.Name, out version);
	}

	internal class CmdLineToolsVersionComparer : IComparer<DirectoryInfo>
	{
		public static CmdLineToolsVersionComparer Default { get; } = new CmdLineToolsVersionComparer();

		public int Compare(DirectoryInfo? x, DirectoryInfo? y)
		{
			var hasX = TryParseCmdlineToolsVersion(x, out var vX);
			var hasY = TryParseCmdlineToolsVersion(y, out var vY);

			if (!hasX && !hasY)
				return 0;
			else if (!hasX)
				return 1;
			else if (!hasY)
				return -1;
			else
				return vX!.CompareTo(vY);
		}

		public DirectoryInfo[] GetSortedDirectories(DirectoryInfo cmdlineToolsPath)
		{
			var dirs = cmdlineToolsPath.GetDirectories();

			Array.Sort(dirs, CmdLineToolsVersionComparer.Default);

			return dirs;
		}
	}
}
