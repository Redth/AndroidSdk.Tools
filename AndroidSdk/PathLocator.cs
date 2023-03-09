#nullable enable
using System.Collections.Generic;
using System.IO;

namespace AndroidSdk;

public abstract class PathLocator : IPathLocator
{
	public virtual string[] AdditionalPaths()
		=>  new string[0];

	public virtual string[] PreferredPaths()
		=> new string[0];

	protected PathLocator() { }

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
			if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c))
				found.Add(new DirectoryInfo(c));
		}

		return found;
	}
}
