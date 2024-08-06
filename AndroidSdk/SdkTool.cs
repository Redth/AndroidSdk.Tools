#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AndroidSdk
{
	public abstract class SdkTool
	{
		public SdkTool()
			: this((SdkToolOptions?)null)
		{
		}

		[Obsolete]
		public SdkTool(string? androidSdkHome)
			: this(new SdkToolOptions { AndroidSdkHome = string.IsNullOrEmpty(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome) })
		{
		}

		[Obsolete]
		public SdkTool(DirectoryInfo? androidSdkHome)
			: this(new SdkToolOptions { AndroidSdkHome = androidSdkHome })
		{
			AndroidSdkHome = new SdkLocator().Locate(androidSdkHome?.FullName)?.FirstOrDefault();
			Jdks = new JdkLocator().LocateJdk()?.ToArray() ?? new JdkInfo[0];
		}

		public SdkTool(SdkToolOptions? options)
		{
			options ??= new();
			AndroidSdkHome = new SdkLocator().Locate(options.AndroidSdkHome?.FullName)?.FirstOrDefault();
			Jdks = new JdkLocator().LocateJdk()?.ToArray() ?? new JdkInfo[0];
		}

		public JdkInfo[] Jdks { get; }

		public DirectoryInfo? AndroidSdkHome { get; internal set; }

		protected bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		public abstract FileInfo? FindToolPath(DirectoryInfo? androidSdkHome = null);

		internal FileInfo? FindTool(DirectoryInfo? androidHome, string toolName, string windowsExtension, params string[] pathSegments)
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

			var ext = isWindows ? windowsExtension : string.Empty;
			var home = new SdkLocator().Locate(androidHome?.FullName)?.FirstOrDefault();

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
