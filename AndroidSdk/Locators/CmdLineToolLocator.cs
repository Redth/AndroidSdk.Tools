#nullable enable
using System.Collections.Generic;
using System.IO;
using static AndroidSdk.SdkManager;

namespace AndroidSdk;

public abstract class CmdLineToolLocator : SdkToolLocator
{
	public override IEnumerable<string[]> GetPathSegments(DirectoryInfo androidSdkHome)
	{
		var pathSegments = new List<string[]>();

		var cmdlineToolsPath = new DirectoryInfo(Path.Combine(androidSdkHome.FullName, "cmdline-tools"));

		if (cmdlineToolsPath.Exists)
		{
			// Sort the directories by version, first using the version in the source.properties
			// file, then by the directory name.
			var dirs = CmdLineToolsVersionComparer.Default.GetSortedDirectories(cmdlineToolsPath);
			foreach (var dir in dirs)
				pathSegments.Insert(0, ["cmdline-tools", dir.Name, "bin"]);
		}

		pathSegments.Add(["tools", "bin"]);

		return pathSegments;
	}
}
