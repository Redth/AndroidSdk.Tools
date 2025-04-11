#nullable enable
using System.Collections.Generic;
using System.IO;
using static AndroidSdk.SdkManager;

namespace AndroidSdk;

public class SdkManagerToolLocator : CmdLineToolLocator
{
	public override string ToolName => "sdkmanager";

	public override string Extension => IsWindows ? ".bat" : string.Empty;
}
