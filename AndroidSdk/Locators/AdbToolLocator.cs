using System.Collections.Generic;
using System.IO;

namespace AndroidSdk;

public class AdbToolLocator : SdkToolLocator
{
	public override string ToolName => "adb";
	public override string Extension => ".exe";

	public override IEnumerable<string[]> GetPathSegments(DirectoryInfo androidSdkHome)
		=> [ (["platform-tools"]) ];
}
