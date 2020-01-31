using Android.Tool.Adb;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Xamarin.AndroidBinderator.Tool
{
	class Program
	{
		public static async Task Main(string[] args)
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
				System.Console.Write("android-tool: ");
				System.Console.WriteLine(e.Message);
				System.Console.WriteLine("Try `android-tool --help' for more information.");
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