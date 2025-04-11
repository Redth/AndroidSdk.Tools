#nullable enable
using System.Collections.Generic;
using System.IO;

namespace AndroidSdk
{
	public interface IPathLocator
	{
		IReadOnlyList<DirectoryInfo> Locate(string? specificHome = null, params string[]? additionalPossibleDirectories);
	}
}
