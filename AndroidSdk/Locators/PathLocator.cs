#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AndroidSdk;

public abstract class PathLocator : IPathLocator
{
	public virtual string[] AdditionalPaths()
		=>  new string[0];

	public virtual string[] PreferredPaths()
		=> new string[0];

	protected PathLocator() { }


	public virtual bool IsValid(string path)
		=> true;

	protected bool IsValidDirectoryPath(string? path)
	{
		var invalidChars = Path.GetInvalidPathChars();

		return !string.IsNullOrWhiteSpace(path) && !path.Any(c => invalidChars.Contains(c));
	}

	protected bool IsValidFilePath(string path)
	{
		var invalidChars = Path.GetInvalidFileNameChars();

		return !string.IsNullOrWhiteSpace(path) && !path.Any(c => invalidChars.Contains(c));
	}

	public IReadOnlyList<DirectoryInfo> Locate(string? specificHome = null, params string[]? additionalPossibleDirectories)
	{
		var found = new List<DirectoryInfo>();
		var candidates = new List<string>();

		if (specificHome is not null && !string.IsNullOrEmpty(specificHome))
			candidates.Add(specificHome);

		var preferred = PreferredPaths();
		foreach (var p in preferred)
		{
			if (!string.IsNullOrWhiteSpace(p))
				candidates.Add(p);
		}

		if (additionalPossibleDirectories != null)
		{
			foreach (var p in additionalPossibleDirectories)
			{
				if (!string.IsNullOrEmpty(p))
					candidates.Add(p);
			}
		}	

		var additional = AdditionalPaths();
		foreach (var p in additional)
		{
			if (!string.IsNullOrWhiteSpace(p))
				candidates.Add(p);
		}

		foreach (var c in candidates)
		{
			if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c) && IsValid(c))
				found.Add(new DirectoryInfo(c));
		}

		return found;
	}
}
