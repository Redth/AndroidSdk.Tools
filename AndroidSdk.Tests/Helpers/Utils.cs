#nullable enable
using System;
using System.IO;

namespace AndroidSdk.Tests;

public static class Utils
{
	public static string TestAssemblyDirectory
	{
		get
		{
			var codeBase = typeof(TestsBase).Assembly.Location;
			var uri = new UriBuilder(codeBase);
			var path = Uri.UnescapeDataString(uri.Path);
			return Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException();
		}
	}

	public static string TestDataDirectory
	{
		get
		{
			//if (System.Diagnostics.Debugger.IsAttached)
			//{
			//	return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
			//		Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "testdata")
			//		: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "testdata");
			//}

			return Path.Combine(TestAssemblyDirectory, "..", "..", "..", "testdata");
		}
	}
}
