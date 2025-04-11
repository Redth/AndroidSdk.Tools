#nullable enable
using Google.Protobuf;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AndroidSdk;

public abstract class SdkToolLocator
{
	protected bool IsWindows
			=> RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
	
	string GetToolNameWithExtension()
	{
		var toolNameAndExtension = ToolName;
		var ext = Extension;

		if (!string.IsNullOrEmpty(ext))
		{
			// Trim off 
			if (ext.StartsWith("."))
				ext = ext.Substring(1);

			toolNameAndExtension = toolNameAndExtension + "." + ext;
		}

		return toolNameAndExtension;
	}

	public abstract string ToolName { get; }
	
	public abstract string Extension { get; }


	public abstract IEnumerable<string[]> GetPathSegments(DirectoryInfo androidSdkHome);

	public virtual FileInfo? FindTool(DirectoryInfo androidSdkHome)
	{
		var toolNameAndExtension = GetToolNameWithExtension();

		foreach (var pathSeg in GetPathSegments(androidSdkHome))
		{
			// Combine Home, Path Segments, and Tool Name/Ext
			var fullPathSegments = new List<string> { androidSdkHome.FullName };
			fullPathSegments.AddRange(pathSeg);
			fullPathSegments.Add(toolNameAndExtension);

			var fullPath = Path.Combine([.. fullPathSegments]);

			if (File.Exists(fullPath))
			{
				return new FileInfo(fullPath);
			}
		}

		return null;
	}
}
