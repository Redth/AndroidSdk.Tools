#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

namespace AndroidSdk
{
	public partial class SdkManager : SdkTool
	{
		const string ANDROID_SDKMANAGER_DEFAULT_ACQUIRE_VERSION = "8.0";
		const string ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED = "7.0";
		const string REPOSITORY_URL_BASE = "https://dl.google.com/android/repository/";
		const string REPOSITORY_URL = REPOSITORY_URL_BASE + "repository2-3.xml";
		const string REPOSITORY_SDK_PATTERN = REPOSITORY_URL_BASE + "commandlinetools-{0}-{1}_latest.zip";
		const string REPOSITORY_SDK_DEFAULT_VERSION = "6858069";

		readonly Regex rxListDesc = new Regex("\\s+Description:\\s+(?<desc>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
		readonly Regex rxListVers = new Regex("\\s+Version:\\s+(?<ver>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
		readonly Regex rxListLoc = new Regex("\\s+Installed Location:\\s+(?<loc>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
		readonly Regex rxLicenseIdLine = new Regex(@"^License\s+(?<id>[a-zA-Z\-]+):$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
		readonly Regex rxPkgRevision = new Regex(@"^Pkg\.Revision=(?<rev>.+)$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		JdkInfo? jdk = null;

		public SdkManager()
			: this((SdkManagerToolOptions?)null)
		{
		}

		[Obsolete]
		public SdkManager(string? androidSdkHome = null, SdkChannel channel = SdkChannel.Stable, bool skipVersionCheck = false, bool includeObsolete = false, SdkManagerProxyOptions? proxy = null)
			: this(new SdkManagerToolOptions { AndroidSdkHome = string.IsNullOrWhiteSpace(androidSdkHome) ? null : new DirectoryInfo(androidSdkHome), Channel = channel, SkipVersionCheck = skipVersionCheck, IncludeObsolete = includeObsolete, Proxy = proxy })
		{
		}

		[Obsolete]
		public SdkManager(DirectoryInfo? androidSdkHome = null, SdkChannel channel = SdkChannel.Stable, bool skipVersionCheck = false, bool includeObsolete = false, SdkManagerProxyOptions? proxy = null)
			: this(new SdkManagerToolOptions { AndroidSdkHome = androidSdkHome, Channel = channel, SkipVersionCheck = skipVersionCheck, IncludeObsolete = includeObsolete, Proxy = proxy })
		{
		}

		public SdkManager(SdkManagerToolOptions? options)
			: base(options)
		{
			options ??= new();

			Channel = options.Channel;
			SkipVersionCheck = options.SkipVersionCheck;
			IncludeObsolete = options.IncludeObsolete;
			Proxy = options.Proxy ?? new SdkManagerProxyOptions();
		}

		public SdkManagerProxyOptions Proxy { get; set; }

		public SdkChannel Channel { get; set; } = SdkChannel.Stable;

		public bool SkipVersionCheck { get; set; }

		public bool IncludeObsolete { get; set; }

		public override FileInfo? FindToolPath(DirectoryInfo? androidSdkHome)
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var ext = isWindows ? ".bat" : string.Empty;

			var likelyPathSegments = new List<string[]>();

			var cmdlineToolsPath = new DirectoryInfo(Path.Combine(androidSdkHome.FullName, "cmdline-tools"));

			if (cmdlineToolsPath.Exists)
			{
				// The custom comparer will make sure that the version named
				// "latest" is treated as the latest version as well as make sure
				// that "11.0" comes after "7.0".
				var dirs = cmdlineToolsPath.GetDirectories()
					.OrderBy(d => d.Name, CmdLineToolsVersionComparer.Default)
					.ToList();
				foreach (var dir in dirs)
				{
					var toolPath = new FileInfo(Path.Combine(dir.FullName, "bin", "sdkmanager" + ext));
					if (toolPath.Exists)
						likelyPathSegments.Insert(0, new[] { "cmdline-tools", dir.Name, "bin" });
				}
			}

			likelyPathSegments.Add(new[] { "tools", "bin" });

			foreach (var pathSeg in likelyPathSegments)
			{
				var tool = FindTool(androidSdkHome, toolName: "sdkmanager", windowsExtension: ".bat", pathSeg);
				if (tool != null)
					return tool;
			}

			return null;
		}

		/// <summary>
		/// Downloads the Android SDK
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="destinationDirectory">Destination directory, or ./tools/androidsdk if none is specified.</param>
		/// <param name="specificVersion">Specific version, or latest if none is specified.</param>
		public async Task DownloadSdk(DirectoryInfo? destinationDirectory = null, string? specificVersion = null, Action<int>? progressHandler = null)
		{
			if (destinationDirectory == null)
				destinationDirectory = AndroidSdkHome;

			if (destinationDirectory == null)
				throw new DirectoryNotFoundException("Android SDK Directory was not specified.");

			if (!destinationDirectory.Exists)
				destinationDirectory.Create();

			var sdkUrl = await GetSdkUrl(specificVersion);

			var sdkDir = new DirectoryInfo(destinationDirectory.FullName);
			if (!sdkDir.Exists)
				sdkDir.Create();

			var sdkZipFile = new FileInfo(Path.Combine(destinationDirectory.FullName, "androidsdk.zip"));

			if (!sdkZipFile.Exists)
			{
				int prevProgress = 0;
				var webClient = new System.Net.WebClient();

				webClient.DownloadProgressChanged += (s, e) =>
				{
					var progress = e.ProgressPercentage;

					if (progress > prevProgress)
						progressHandler?.Invoke(progress);

					prevProgress = progress;
				};
				await webClient.DownloadFileTaskAsync(sdkUrl, sdkZipFile.FullName);
			}

			using (var zip = ZipFile.OpenRead(sdkZipFile.FullName))
			{
				// Read the revision from the source.properties file
				var toolsVersion = ANDROID_SDKMANAGER_DEFAULT_ACQUIRE_VERSION;
				try
				{
					var sourceProperties = zip.GetEntry("cmdline-tools/source.properties");
					if (sourceProperties is not null)
					{
						using var stream = sourceProperties.Open();
						using var reader = new StreamReader(stream);
						string? line;
						while ((line = reader.ReadLine()) is not null)
						{
							var matched = rxPkgRevision.Match(line)?.Groups?["rev"]?.Value?.Trim();
							if(!string.IsNullOrWhiteSpace(matched))
							{
								toolsVersion = matched;
							}
						}
					}
				}
				catch
				{
					// Something went wrong, but it does not really matter
				}

				foreach (var entry in zip.Entries)
				{
					var name = entry.FullName;
					if (name.StartsWith("cmdline-tools"))
						name = $"cmdline-tools/{toolsVersion}" + name.Substring(13);

					name = name.Replace('/', Path.DirectorySeparatorChar);

					var dest = Path.Combine(sdkDir.FullName, name);

					if (string.IsNullOrWhiteSpace(entry.Name))
					{
						var dirInfo = new DirectoryInfo(dest);
						dirInfo.Create();
					}
					else
					{
						var fileInfo = new FileInfo(dest);
						fileInfo.Directory?.Create();

						entry.ExtractToFile(dest, true);
					}
				}
			}
		}

		static async Task<string> GetSdkUrl(string? specificVersion)
		{
			string platformStr;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				platformStr = "win";
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				platformStr = "mac";
			else
				platformStr = "linux";

			string sdkUrl = "";

			// Download the repository xml so we can check for versions in there
			var xdoc = await DownloadRepositoryXml();

			// The user specified a specific version
			if (!string.IsNullOrWhiteSpace(specificVersion))
			{
				// Maybe 'latest' or '7.0' or '8.0' etc (only if our XML downloaded)
				if (xdoc is not null)
				{
					try
					{
						var urlNode = xdoc.SelectSingleNode($"//remotePackage[@path='cmdline-tools;{specificVersion}']/archives/archive/complete/url[contains(text(),'{platformStr}')]");
						if (urlNode is not null)
							sdkUrl = REPOSITORY_URL_BASE + urlNode.InnerText;
					}
					catch
					{
						// maybe the number is not valid for XML but still may be a URL number
					}
				}

				// Assume the user passed a specific version number
				if (string.IsNullOrWhiteSpace(sdkUrl))
				{
					sdkUrl = string.Format(REPOSITORY_SDK_PATTERN, platformStr, specificVersion);
				}
			}
			else
			{
				// The user did not specify a version

				// Try to get the default version from the XML
				if (xdoc is not null)
				{
					var urlNode = xdoc.SelectSingleNode($"//remotePackage[@path='cmdline-tools;{ANDROID_SDKMANAGER_DEFAULT_ACQUIRE_VERSION}']/archives/archive/complete/url[contains(text(),'{platformStr}')]");
					sdkUrl = REPOSITORY_URL_BASE + urlNode.InnerText;
				}

				// Fall back to the default version in this library
				if (string.IsNullOrWhiteSpace(sdkUrl))
				{
					sdkUrl = string.Format(REPOSITORY_SDK_PATTERN, platformStr, REPOSITORY_SDK_DEFAULT_VERSION);
				}
			}

			return sdkUrl;
		}

		static async Task<XmlDocument?> DownloadRepositoryXml()
		{
			using var http = new HttpClient();
			http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
			http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
			http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
			http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

			try
			{
				var data = await http.GetStringAsync(REPOSITORY_URL);

				var xdoc = new XmlDocument();
				xdoc.LoadXml(data);
				return xdoc;
			}
			catch
			{
			}

			return null;
		}

		public bool IsUpToDate()
		{
			if (SkipVersionCheck)
				return true;

			var v = GetVersion();

			var min = Version.Parse(ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED);

			if (v == null || v < min)
				return false;

			return true;
		}

		public Version? GetVersion()
		{
			if (AndroidSdkHome?.Exists != true)
				return null;

			var builder = new ProcessArgumentBuilder();
			builder.Append("--version");

			var p = run(false, builder);

			if (p != null)
			{
				foreach (var l in p)
				{
					if (Version.TryParse(l?.Trim() ?? string.Empty, out var v))
						return v;
				}
			}

			return null;
		}

		internal void CheckSdkManagerVersion()
		{
			if (SkipVersionCheck)
				return;

			if (!IsUpToDate())
				throw new NotSupportedException("Your sdkmanager is out of date.  Version " + ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED + " or later is required.");
		}

		public SdkManagerList List()
		{
			var result = new SdkManagerList();

			CheckSdkManagerVersion();

			var builder = new ProcessArgumentBuilder();

			builder.Append("--list --verbose");

			BuildStandardOptions(builder);

			var p = run(false, builder);

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
					if (string.IsNullOrEmpty(path))
					{

						// If we have spaces preceding the line, it's not a new item yet
						if (line.StartsWith(" "))
							continue;

						path = line.Trim();
						continue;
					}

					if (rxListDesc.IsMatch(line))
					{
						description = rxListDesc.Match(line)?.Groups?["desc"]?.Value;
						continue;
					}

					if (rxListVers.IsMatch(line))
					{
						version = rxListVers.Match(line)?.Groups?["ver"]?.Value;
						continue;
					}

					if (rxListLoc.IsMatch(line))
					{
						location = rxListLoc.Match(line)?.Groups?["loc"]?.Value;
						// No need to continue here since this is the last line in the output for an item
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

		public bool Install(params string[] packages)
			=> InstallOrUninstall(true, packages);

		public bool Uninstall(params string[] packages)
			=> InstallOrUninstall(false, packages);

		internal bool InstallOrUninstall(bool install, IEnumerable<string> packages)
		{
			CheckSdkManagerVersion();

			var builder = new ProcessArgumentBuilder();

			if (!install)
				builder.Append("--uninstall");

			foreach (var pkg in packages)
				builder.AppendQuoted(pkg);

			BuildStandardOptions(builder);

			var output = run(true, builder);

			return true;
		}

		public bool AcceptLicenses()
		{
			CheckSdkManagerVersion();

			var builder = new ProcessArgumentBuilder();

			builder.Append("--licenses");

			BuildStandardOptions(builder);

			run(true, builder);

			return true;
		}

		public IEnumerable<string> GetAcceptedLicenseIds()
		{
			var ids = new List<string>();

			var licensesDir = new DirectoryInfo(Path.Combine(AndroidSdkHome.FullName, "licenses"));

			if (licensesDir.Exists)
			{
				var licenseFiles = licensesDir.GetFiles();

				foreach (var lf in licenseFiles)
				{
					if ((File.ReadAllText(lf.FullName)?.Trim()?.Length ?? 0) == 40)
					{
						ids.Add(Path.GetFileNameWithoutExtension(lf.FullName));
					}
				}
			}

			return ids;
		}

		public IEnumerable<SdkLicense> GetLicenses()
		{
			CheckSdkManagerVersion();

			var builder = new ProcessArgumentBuilder();

			builder.Append("--licenses");

			BuildStandardOptions(builder);

			var lines = run(false, builder, true, false);

			return ParseLicenseCommandOutput(lines);
		}

		internal List<SdkLicense> ParseLicenseCommandOutput(IEnumerable<string> lines)
		{
			var licenses = new List<SdkLicense>();

			SdkLicense? license = null;

			foreach (var line in lines)
			{
				var idMatch = rxLicenseIdLine.Match(line)?.Groups?["id"]?.Value;

				// Is this a license header line
				if (!string.IsNullOrEmpty(idMatch))
				{
					// Is there an existing license being parsed? add it to the results
					if (license is not null)
					{
						licenses.Add(license);
						license = null;
					}

					license = new SdkLicense();
					license.Id = idMatch;

					continue;
				}
				else
				{
					// Until the first license Id is parsed, everything is throwaway
					if (license is null)
						continue;

					// HR lines are unnecessary
					if (line.StartsWith("---") && line.EndsWith("---"))
						continue;

					// Let's see if the license was accepted
					if (line.StartsWith("Accepted", StringComparison.OrdinalIgnoreCase))
					{
						license.Accepted = true;
						continue;
					}

					// Add the line of text
					license.License.Add(line);
				}
			}

			if (license is not null)
				licenses.Add(license);

			return licenses;
		}

		public bool UpdateAll()
		{
			var sdkManager = FindToolPath(AndroidSdkHome);

			if (!(sdkManager?.Exists ?? false))
				throw new FileNotFoundException("Could not locate sdkmanager", sdkManager?.FullName);

			var builder = new ProcessArgumentBuilder();

			builder.Append("--update");

			BuildStandardOptions(builder);

			var o = run(true, builder);

			return true;
		}

		public IEnumerable<string> Help()
		{
			return run(false, new ProcessArgumentBuilder());
		}


		IEnumerable<string> run(bool withAccept, ProcessArgumentBuilder args, bool includeStdOut = true, bool includeStdErr = true)
		{
			jdk ??= Jdks.FirstOrDefault();
			if (jdk is null)
				throw new InvalidOperationException("Unable to find the JDK.");

			var sdkManager = FindToolPath(AndroidSdkHome);
			var java = jdk.Java;

			var libPath = Path.GetFullPath(Path.Combine(sdkManager.DirectoryName, "..", "lib"));
			var toolPath = Path.GetFullPath(Path.Combine(sdkManager.DirectoryName, ".."));

			var cpSeparator = IsWindows ? ";" : ":";

			// Get all the .jars in the tools\lib folder to use as classpath
			//var classPath = "avdmanager-classpath.jar";
			var classPath = string.Join(cpSeparator, Directory.GetFiles(libPath, "*.jar").Select(f => new FileInfo(f).Name));

			var proc = new Process();
			// This is the package and class that contains the main() for avdmanager
			proc.StartInfo.Arguments = "com.android.sdklib.tool.sdkmanager.SdkManagerCli " + args.ToString();
			// This needs to be set to the working dir / classpath dir as the library looks for this system property at runtime
			//proc.StartInfo.Environment["JAVA_TOOL_OPTIONS"] = $"-Dcom.android.sdkmanager.toolsdir=\"{toolPath}\"";
			proc.StartInfo.Environment["JAVA_TOOL_OPTIONS"] = $"-Dcom.android.sdklib.toolsdir=\"{toolPath}\"";
			// Set the classpath to all the .jar files we found in the lib folder
			proc.StartInfo.Environment["CLASSPATH"] = classPath;

			// Java.exe
			proc.StartInfo.FileName = java.FullName;

			// lib folder is our working dir
			proc.StartInfo.WorkingDirectory = libPath;

			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;
			proc.StartInfo.RedirectStandardInput = true;

			var output = new List<string>();
			var stderr = new List<string>();
			var stdout = new List<string>();

			proc.OutputDataReceived += (s, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
				{
					if (includeStdOut)
						output.Add(e.Data);
					stdout.Add(e.Data);
				}
			};
			proc.ErrorDataReceived += (s, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
				{
					if (includeStdErr)
						output.Add(e.Data);
					stderr.Add(e.Data);
				}
			};

			var cmd = $"{proc.StartInfo.FileName} {proc.StartInfo.Arguments}";

			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();

			// continuously send "y" to accept any licenses
			while (!proc.HasExited)
			{
				Thread.Sleep(250);

				try
				{
					proc.StandardInput.WriteLine("y");
					proc.StandardInput.Flush();
				}
				catch { }
			}

			proc.WaitForExit();

			if (proc.ExitCode != 0)
				throw new SdkToolFailedExitException("avdmanager", proc.ExitCode, stderr, stdout);

			return output;
		}

		void BuildStandardOptions(ProcessArgumentBuilder builder)
		{
			builder.Append("--verbose");

			if (Channel != SdkChannel.Stable)
				builder.Append("--channel=" + (int)Channel);

			if (AndroidSdkHome?.Exists ?? false)
				builder.Append($"--sdk_root=\"{AndroidSdkHome.FullName}\"");

			if (IncludeObsolete)
				builder.Append("--include_obsolete");

			if (Proxy.NoHttps)
				builder.Append("--no_https");

			if (Proxy.ProxyType != SdkManagerProxyType.None)
			{
				builder.Append($"--proxy={Proxy.ProxyType.ToString().ToLower()}");

				if (!string.IsNullOrEmpty(Proxy.ProxyHost))
					builder.Append($"--proxy_host=\"{Proxy.ProxyHost}\"");

				if (Proxy.ProxyPort > 0)
					builder.Append($"--proxy_port=\"{Proxy.ProxyPort}\"");
			}
		}

		public async Task Acquire()
		{
			var sdkManagerApp = FindToolPath(AndroidSdkHome);

			// Download if it doesn't exist
			if (sdkManagerApp == null || !sdkManagerApp.Exists)
			{
				await DownloadSdk(AndroidSdkHome, null, null);
				sdkManagerApp = FindToolPath(AndroidSdkHome);
			}

			UpdateAll();
		}

		static string GetRelativePath(string fromPath, string toPath)
		{
			var fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
			var toUri = new Uri(AppendDirectorySeparatorChar(toPath));
			var relativeUri = fromUri.MakeRelativeUri(toUri);
			var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
			return relativePath;
		}

		// Append a slash only if the path is a directory and does not have a slash.
		static string AppendDirectorySeparatorChar(string path) =>
			Path.HasExtension(path) || path.EndsWith(Path.DirectorySeparatorChar.ToString())
				? path
				: path + Path.DirectorySeparatorChar;

		internal class CmdLineToolsVersionComparer : IComparer<string>
		{
			public static CmdLineToolsVersionComparer Default { get; } = new CmdLineToolsVersionComparer();

			public int Compare(string? x, string? y)
			{
				if (!Version.TryParse(x, out var vX))
					return 1;
				if (!Version.TryParse(y, out var vY))
					return -1;
				return vX.CompareTo(vY);
			}
		}
	}
}
