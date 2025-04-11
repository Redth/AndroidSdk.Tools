#nullable enable
using System.Collections.Generic;
using System.IO;

namespace AndroidSdk;

public class AvdManagerToolLocator : CmdLineToolLocator
{
	public override string ToolName => "avdmanager";

	public override string Extension => IsWindows ? ".bat" : string.Empty;
}