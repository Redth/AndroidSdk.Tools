using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

namespace AndroidSdk;

public partial class SdkManager : JdkTool
{
	const string ANDROID_SDKMANAGER_MINIMUM_VERSION_REQUIRED = "13.0";

	readonly Regex rxListDesc = new Regex("\\s+Description:\\s+(?<desc>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
	readonly Regex rxListVers = new Regex("\\s+Version:\\s+(?<ver>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
	readonly Regex rxListLoc = new Regex("\\s+Installed Location:\\s+(?<loc>.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
	readonly Regex rxLicenseIdLine = new Regex(@"^License\s+(?<id>[a-zA-Z\-]+):$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public SdkManager(DirectoryInfo androidSdkHome, DirectoryInfo javaHome, SdkManagerToolOptions? options = null)
		: base(androidSdkHome, javaHome)
	{
		Channel = options?.Channel ?? SdkChannel.Stable;
		SkipVersionCheck = options?.SkipVersionCheck ?? false;
		IncludeObsolete = options?.IncludeObsolete ?? false;
		Proxy = options?.Proxy ?? new SdkManagerProxyOptions();
	}

	public SdkManagerProxyOptions Proxy { get; set; }

	public SdkChannel Channel { get; set; } = SdkChannel.Stable;

	public bool SkipVersionCheck { get; set; }

	public bool IncludeObsolete { get; set; }

	public bool CanModify()
	{
		try
		{
			var path = AndroidSdkHome?.FullName;

			if (string.IsNullOrEmpty(path))
				return false;

			// Check if path is a file or directory
			if (File.Exists(path))
				path = Path.GetDirectoryName(path); // Get the directory of the file

			if (!Directory.Exists(path))
				Directory.CreateDirectory(path!);

			string tempFile = Path.Combine(path!, Path.GetRandomFileName());

			// Try creating and deleting a file
			using (FileStream fs = File.Create(tempFile, 1, FileOptions.DeleteOnClose)) { }

			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool IsUpToDate()
	{
		if (AndroidSdkHome is null)
			return false;

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

		var p = Run(builder);

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

		var p = Run(builder);

		int section = 0;

		string? path = null;
		string? description = null;
		string? version = null;
		string? location = null;
		
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
					if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(location))
						result.InstalledPackages.Add(new InstalledSdkPackage(path!, version!, location!, description));
				}
				else if (section == 2)
				{
					if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(version))
						result.AvailablePackages.Add(new SdkPackage(path!, version!, description));
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

		var output = RunWithAcceptLoop(builder);

		return true;
	}

	public bool AcceptLicenses()
	{
		CheckSdkManagerVersion();

		var builder = new ProcessArgumentBuilder();

		builder.Append("--licenses");

		BuildStandardOptions(builder);

		RunWithAcceptLoop(builder);

		return true;
	}

	public IEnumerable<string> GetAcceptedLicenseIds()
	{
		var ids = new List<string>();

		if (AndroidSdkHome is null)
			return ids;

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

		var lines = Run(builder, true, false);

		return ParseLicenseCommandOutput(lines);
	}

	internal List<SdkLicense> ParseLicenseCommandOutput(IEnumerable<string> lines)
	{
		var licenses = new List<SdkLicense>();

		SdkLicense? license = null;

		foreach (var line in lines)
		{
			if (line.StartsWith("All SDK package licenses accepted."))
				continue;

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

				license = new SdkLicense(idMatch!);

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
		var builder = new ProcessArgumentBuilder();

		builder.Append("--update");

		BuildStandardOptions(builder);

		var o = RunWithAcceptLoop(builder);

		return true;
	}

	public IEnumerable<string> Help()
	{
		return Run(new());
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

			if (Proxy.ProxyPort.HasValue && Proxy.ProxyPort.Value > 0)
				builder.Append($"--proxy_port=\"{Proxy.ProxyPort.Value}\"");
		}
	}		

	IEnumerable<string> Run(ProcessArgumentBuilder args, bool includeStdOut = true, bool includeStdErr = true)
	{
		var runner = Start(args);

		var result = WaitForExit(runner);

		if (includeStdOut && includeStdErr)
			return result.Output;
		else if (includeStdOut)
			return result.StandardOutput;
		else if (includeStdErr)
			return result.StandardError;
		else
			return Array.Empty<string>();
	}

	IEnumerable<string> RunWithAcceptLoop(ProcessArgumentBuilder args, bool includeStdOut = true, bool includeStdErr = true)
	{
		var runner = Start(args, true);

		// continuously send "y" to accept any licenses
		runner.WriteContinuouslyUntilExit("y");

		var result = WaitForExit(runner);

		if (includeStdOut && includeStdErr)
			return result.Output;
		else if (includeStdOut)
			return result.StandardOutput;
		else if (includeStdErr)
			return result.StandardError;
		else
			return Array.Empty<string>();
	}

	JavaProcessRunner Start(ProcessArgumentBuilder args, bool redirectStandardInput = false)
	{
		var sdkManagerLocator = new SdkManagerToolLocator();

		var sdkManager = sdkManagerLocator.FindTool(AndroidSdkHome);

		if (sdkManager is null || sdkManager.DirectoryName is null || !sdkManager.Exists)
			throw new FileNotFoundException($"Could not locate {sdkManagerLocator.ToolName}", sdkManager?.FullName);

		var libPath = Path.GetFullPath(Path.Combine(sdkManager.DirectoryName, "..", "lib"));
		var toolPath = Path.GetFullPath(Path.Combine(sdkManager.DirectoryName, ".."));

		// This is the package and class that contains the main() for avdmanager
		var javaArgs = new JavaProcessArgumentBuilder("com.android.sdklib.tool.sdkmanager.SdkManagerCli", args);

		// Set the classpath to all the .jar files we found in the lib folder
		javaArgs.AppendClassPath(Directory.GetFiles(libPath, "*.jar").Select(f => new FileInfo(f).Name));

		// This needs to be set to the working dir / classpath dir as the library looks for this system property at runtime
		javaArgs.AppendJavaToolOptions($"-Dcom.android.sdklib.toolsdir=\"{toolPath}\"");

		// lib folder is our working dir
		javaArgs.SetWorkingDirectory(libPath);

		var runner = new JavaProcessRunner(JdkHome, javaArgs, default, redirectStandardInput);

		return runner;
	}

	ProcessResult WaitForExit(JavaProcessRunner runner)
	{
		var result = runner.WaitForExit();

		//SdkToolFailedExitException.ThrowIfErrorExitCode("sdkmanager", result);

		return result;
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
}
