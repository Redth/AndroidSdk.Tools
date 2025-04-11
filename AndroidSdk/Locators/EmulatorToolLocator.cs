#nullable enable
using System.Collections.Generic;
using System.IO;

namespace AndroidSdk;

public class EmulatorToolLocator : SdkToolLocator
{
	public override string ToolName => "emulator";
	public override string Extension => IsWindows ? "exe" : string.Empty;

	public override IEnumerable<string[]> GetPathSegments(DirectoryInfo androidSdkHome)
		=>
		[
			(["emulator"]),
			(["tools", "bin"])
		];
}
