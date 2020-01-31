using System;
using System.Collections.Generic;
using System.Text;

namespace Android.Tool
{
	internal class ProcessArgumentBuilder
	{
		public List<string> args = new List<string>();

		public void Append(string arg)
			=> args.Add(arg);

		public void AppendQuoted(string arg)
			=> args.Add($"\"{arg}\"");

		public override string ToString()
			=> string.Join(" ", args);
	}
}
