using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AndroidSdk.Tool
{
	class Program
	{
		public static void Main(string[] args)
		{
			var serial = string.Empty;
			var shouldShowHelp = false;

			// thses are the available options, not that they set the variables
			var options = new OptionSet {
				{ "s|serial=", "Android Device or Emulator Serial", s => serial = s },
				{ "h|help", "show this message and exit", h => shouldShowHelp = h != null },
			};

			List<string> extra;
			try
			{
				// parse the command line
				extra = options.Parse(args);
			}
			catch (OptionException e)
			{
				// output some error message
				Console.Write("android-tool: ");
				Console.WriteLine(e.Message);
				Console.WriteLine("Try `android-tool --help' for more information.");
				return;
			}

			if (shouldShowHelp)
			{
				options.WriteOptionDescriptions(Console.Out);
				return;
			}
		}
	}
}