using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Android.Tool
{
	public partial class SdkManager
	{
		const string ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED = "26.1.1";

		readonly Regex rxListDesc = new Regex("\\s+Description:\\s+(?<desc>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
		readonly Regex rxListVers = new Regex("\\s+Version:\\s+(?<ver>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
		readonly Regex rxListLoc = new Regex("\\s+Installed Location:\\s+(?<loc>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);

		public SdkManager()
			: this(new SdkManagerOptions())
		{ }
		
		public SdkManager(SdkManagerOptions options)
		{
			Options = options;
		}

		public SdkManager(DirectoryInfo androidSdkHome)
			: this(new SdkManagerOptions { AndroidSdkHome = androidSdkHome })
		{
		}

		public SdkManager(string androidSdkHome)
		: this(new SdkManagerOptions { AndroidSdkHome = new DirectoryInfo(androidSdkHome) })
		{
		}

		public SdkManagerOptions Options { get; set; }

		public bool IsUpToDate()
		{
			if (Options.SkipVersionCheck)
				return true;

			var builder = new ProcessArgumentBuilder();
			builder.Append("--version");

			var p = Run(builder);

			if (!p.Any(o => o.Trim().Equals(ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED, StringComparison.OrdinalIgnoreCase)))
				return false;

			return true;
		}

		internal void CheckSdkManagerVersion ()
		{
			if (Options.SkipVersionCheck)
				return;
			
			if (!IsUpToDate())
				throw new NotSupportedException("Your sdkmanager is out of date.  Version " + ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED + " or later is required.");
		}

		public SdkManagerList List()
		{
			var result = new SdkManagerList();

			CheckSdkManagerVersion();

			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			builder.Append("--list --verbose");

			BuildStandardOptions(builder);

			var p = Run(builder);

			int section = 0;

			var path = string.Empty;
			var description = string.Empty;
			var version = string.Empty;
			var location = string.Empty;

			foreach (var line in p)
			{
				if (line.StartsWith("------"))
					continue;
				
				if (line.ToLowerInvariant().Contains("installed packages:"))
				{
					section = 1;
					continue;
				}
				else if (line.ToLowerInvariant().Contains("available packages:"))
				{
					section = 2;
					continue;
				}
				else if (line.ToLowerInvariant().Contains("available updates:"))
				{
					section = 3;
					continue;
				}

				if (section >= 1 && section <= 2)
				{
					if (string.IsNullOrEmpty(path)) {

						// If we have spaces preceding the line, it's not a new item yet
						if (line.StartsWith(" "))
							continue;
						
						path = line.Trim();
						continue;
					}

					if (rxListDesc.IsMatch(line)) {
						description = rxListDesc.Match(line)?.Groups?["desc"]?.Value;
						continue;
					}

					if (rxListVers.IsMatch(line)) {
						version = rxListVers.Match(line)?.Groups?["ver"]?.Value;
						continue;
					}

					if (rxListLoc.IsMatch(line)) {
						location = rxListLoc.Match(line)?.Groups?["loc"]?.Value;
						continue;
					}

					// If we got here, we should have a good line of data
					if (section == 1)
					{
						result.InstalledPackages.Add(new InstalledSdkPackage
						{
							Path = path,
							Version = version,
							Description = description,
							Location = location
						});
					}
					else if (section == 2)
					{
						result.AvailablePackages.Add(new SdkPackage
						{
							Path = path,
							Version = version,
							Description = description
						});
					}

					path = null;
					description = null;
					version = null;
					location = null;
				}
			}

			return result;
		}


		public bool InstallOrUninstall(bool install, IEnumerable<string> packages)
		{
			CheckSdkManagerVersion();

			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			if (!install)
				builder.Append("--uninstall");
			
			foreach (var pkg in packages)
				builder.AppendQuoted(pkg);

			BuildStandardOptions(builder);

			RunWithAccept(builder);

			return true;
		}

		public bool AcceptLicenses()
		{
			CheckSdkManagerVersion();

			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			builder.Append("--licenses");

			BuildStandardOptions(builder);

			RunWithAccept(builder);

			return true;
		}

		public bool UpdateAll()
		{
			var sdkManager = AndroidSdk.FindSdkManager(Options?.AndroidSdkHome);

			if (!(sdkManager?.Exists ?? false))
				throw new FileNotFoundException("Could not locate sdkmanager", sdkManager?.FullName);

			//adb devices -l
			var builder = new ProcessArgumentBuilder();

			builder.Append("update");

			BuildStandardOptions(builder);

			RunWithAccept(builder);

			return true;
		}

		public IEnumerable<string> Help()
		{
			//adb devices -l
			return Run(new ProcessArgumentBuilder());
		}

		List<string> RunWithAccept(ProcessArgumentBuilder builder)
			=> RunWithAccept(builder, TimeSpan.FromSeconds(100));

		List<string> RunWithAccept(ProcessArgumentBuilder builder, TimeSpan timeout)
		{
			var sdkManager = AndroidSdk.FindSdkManager(Options?.AndroidSdkHome);

			if (!(sdkManager?.Exists ?? false))
				throw new FileNotFoundException("Could not locate sdkmanager", sdkManager?.FullName);

			var ct = new CancellationTokenSource();
			if (timeout != TimeSpan.Zero)
				ct.CancelAfter(timeout);

			var p = new ProcessRunner(sdkManager, builder, ct.Token, true);

			while (!p.HasExited)
			{
				Thread.Sleep(250);
				p.StandardInputWriteLine("y");
			}

			var r = p.WaitForExit();

			return r.StandardOutput;
		}

		List<string> Run(ProcessArgumentBuilder builder)
		{
			var sdkManager = AndroidSdk.FindSdkManager(Options?.AndroidSdkHome);

			if (!(sdkManager?.Exists ?? false))
				throw new FileNotFoundException("Could not locate sdkmanager", sdkManager?.FullName);

			var p = new ProcessRunner(sdkManager, builder);

			var r = p.WaitForExit();

			return r.StandardOutput;
		}

		void BuildStandardOptions(ProcessArgumentBuilder builder)
		{
			builder.Append("--verbose");

			if (Options.Channel != SdkChannel.Stable)
				builder.Append("--channel=" + (int)Options.Channel);

			if (Options.AndroidSdkHome != null && Options.AndroidSdkHome.Exists)
				builder.Append($"--sdk_root=\"{Options.AndroidSdkHome.FullName}\"");

			if (Options.IncludeObsolete)
				builder.Append("--include_obsolete");

			if (Options.NoHttps)
				builder.Append("--no_https");

			if (Options.ProxyType != SdkManagerProxyType.None)
			{
				builder.Append($"--proxy={Options.ProxyType.ToString().ToLower()}");

				if (!string.IsNullOrEmpty(Options.ProxyHost))
					builder.Append($"--proxy_host=\"{Options.ProxyHost}\"");

				if (Options.ProxyPort > 0)
					builder.Append($"--proxy_port=\"{Options.ProxyPort}\"");
			}
		}
	}
}
