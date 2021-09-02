using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AndroidSdk
{
	public class SdkToolFailedExitException : Exception
	{
		public SdkToolFailedExitException(string name, int exitCode, IEnumerable<string> stdErr, IEnumerable<string> stdOut)
			: base($"{name} exited with an error status")
		{
			ExitCode = exitCode;
			StdErr = stdErr?.ToArray() ?? new string[0];
			StdOut = stdOut?.ToArray() ?? new string[0];
		}

		public readonly int ExitCode;
		public readonly string[] StdOut;
		public readonly string[] StdErr;

		public IEnumerable<string> AllOutput
			=> StdOut.Concat(StdErr);
	}


	public abstract class SdkTool
	{
		public SdkTool()
			: this((string)null)
		{
		}

		public SdkTool(string androidSdkHome)
			: this(string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome))
		{
		}

		public SdkTool(DirectoryInfo androidSdkHome)
		{
			AndroidSdkHome = AndroidSdkManager.FindHome(androidSdkHome)?.FirstOrDefault();
			Jdks = new JdkLocator().Find()?.ToArray() ?? new JdkInfo[0];
		}

		public JdkInfo[] Jdks { get; private set; }

		internal abstract string SdkPackageId { get; }

		public DirectoryInfo AndroidSdkHome { get; internal set; }

		protected bool IsWindows
			=> RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		public abstract FileInfo FindToolPath(DirectoryInfo androidSdkHome = null);

		internal FileInfo FindTool(DirectoryInfo androidHome, string toolName, string windowsExtension, params string[] pathSegments)
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			var ext = isWindows ? windowsExtension : string.Empty;
			var home = AndroidSdkManager.FindHome(androidHome)?.FirstOrDefault();

			if (home?.Exists ?? false)
			{
				var allSegments = new List<string>();
				allSegments.Add(home.FullName);
				allSegments.AddRange(pathSegments);
				allSegments.Add(toolName + ext);

				var tool = Path.Combine(allSegments.ToArray());

				if (File.Exists(tool))
					return new FileInfo(tool);
			}

			return null;
		}
	}
}
