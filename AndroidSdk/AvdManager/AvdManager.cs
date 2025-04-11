using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace AndroidSdk;

public partial class AvdManager(DirectoryInfo androidSdkHome, DirectoryInfo jdkHome) : JdkTool(androidSdkHome, jdkHome)
{
	public void Create(string name, string sdkId, string? device = null, string? path = null, bool force = false, string? sdCardPath = null, string? sdCardSize = null) =>
		Create(name, sdkId, new AvdCreateOptions { Device = device, Path = path, Force = force, SdCardPathOrSize = string.IsNullOrEmpty(sdCardPath) ? sdCardSize : sdCardPath });

	public void Create(string name, string sdkId, AvdCreateOptions options)
	{
		if (options == null)
			options = new AvdCreateOptions();

		var args = new ProcessArgumentBuilder();
		args.Append("create");
		args.Append("avd");
		args.Append("-n");
		args.AppendQuoted(name);
		args.Append("-k");
		args.AppendQuoted(sdkId);

		if (!string.IsNullOrEmpty(options.Abi))
		{
			args.Append("-b");
			args.AppendQuoted(options.Abi!);
		}

		if (!string.IsNullOrEmpty(options.Device))
		{
			args.Append("--device");
			args.AppendQuoted(options.Device!);
		}

		if (!string.IsNullOrEmpty(options.SdCardPathOrSize))
		{
			args.Append("-c");
			args.AppendQuoted(options.SdCardPathOrSize!);
		}

		if (!string.IsNullOrEmpty(options.Skin))
		{
			args.Append("--skin");
			args.AppendQuoted(options.Skin!);
		}

		if (options.Force)
			args.Append("--force");

		if (!string.IsNullOrEmpty(options.Path))
		{
			args.Append("-p");
			args.AppendQuoted(options.Path!);
		}

		var p = Start(args, true);

		while (!p.HasExited)
		{
			Thread.Sleep(250);
			try
			{
				// Continuously send "ENTER" to respond to the prompt:
				// "Do you wish to create a custom hardware profile? [no]"
				// Sending "no" too soon will cause an error...
				p.StandardInputWriteLine(string.Empty);
				p.StandardInputFlush();
			}
			catch { }
		}

		var r = p.WaitForExit();

		if (r.ExitCode != 0)
		{
			throw new InvalidOperationException($"Failed to create AVD '{name}' with error code {r.ExitCode}.\n{string.Join("\n", r.Output)}");
		}
	}

	public void Delete(string name)
	{
		var args = new ProcessArgumentBuilder();
		args.Append("delete");
		args.Append("avd");
		args.Append("-n");
		args.AppendQuoted(name);

		Run(args);
	}

	public void Move(string name, string? path = null, string? newName = null)
	{

		var args = new ProcessArgumentBuilder();
		args.Append("move");
		args.Append("avd");
		args.Append("-n");
		args.AppendQuoted(name);

		if (!string.IsNullOrEmpty(path))
		{
			args.Append("-p");
			args.AppendQuoted(path!);
		}

		if (!string.IsNullOrEmpty(newName))
		{
			args.Append("-r");
			args.AppendQuoted(newName!);
		}

		Run(args);
	}

	static Regex rxListTargets = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+Type\s?:\s+(?<type>[^\n]+)\s+API level\s?:\s+(?<api>[^\n]+)\s+Revision\s?:\s+(?<revision>[^\n]+)", RegexOptions.Multiline | RegexOptions.Compiled);

	public IEnumerable<AvdTarget> ListTargets()
	{
		var r = new List<AvdTarget>();

		var args = new ProcessArgumentBuilder();
		args.Append("list");
		args.Append("target");

		var lines = Run(args);

		var str = string.Join("\n", lines);

		var matches = rxListTargets.Matches(str);
		if (matches != null && matches.Count > 0)
		{
			foreach (Match m in matches)
			{
				// Parse out the id further from: `18 or "pixel"`
				var idstr = m.Groups?["id"]?.Value;

				if (string.IsNullOrEmpty(idstr))
					continue;

				int? idnum = null;

				var idRx = rxIdOr.Match(idstr);

				if (idRx != null && idRx.Success && int.TryParse(idRx.Groups?["num"]?.Value, out var idInt))
				{
					idnum = idInt;
					idstr = idRx.Groups?["str"]?.Value ?? idstr;
				}

				var name = m.Groups?["name"]?.Value;
				var type = m.Groups?["type"]?.Value;

				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type))
					continue;

				var a = new AvdTarget(idstr!, idnum, name!, type!);

				if (int.TryParse(m.Groups?["api"]?.Value, out var api))
					a.ApiLevel = api;
				if (int.TryParse(m.Groups?["revision"]?.Value, out var rev))
					a.Revision = rev;

				if (!string.IsNullOrWhiteSpace(a.Id) && a.ApiLevel > 0)
					r.Add(a);
			}
		}

		return r;
	}

	static Regex rxListAvds = new Regex(@"\s+Name:\s+(?<name>[^\n]+)(\s+Device:\s+(?<device>[^\n]+))?\s+Path:\s+(?<path>[^\n]+)\s+Target:\s+(?<target>[^\n]+)\s+Based on:\s+(?<basedon>[^\n]+)", RegexOptions.Compiled | RegexOptions.Multiline);
	public IEnumerable<Avd> ListAvds()
	{
		var r = new List<Avd>();

		var args = new ProcessArgumentBuilder();
		args.Append("list");
		args.Append("avd");

		var lines = Run(args);

		var str = string.Join("\n", lines);

		var matches = rxListAvds.Matches(str);
		if (matches != null && matches.Count > 0)
		{
			foreach (Match m in matches)
			{
				var name = m.Groups?["name"]?.Value;
				var device = m.Groups?["device"]?.Value;
				var path = m.Groups?["path"]?.Value;
				var target = m.Groups?["target"]?.Value;
				var basedOn = m.Groups?["basedon"]?.Value;

				if (string.IsNullOrEmpty(name)
					|| string.IsNullOrEmpty(device)
					|| string.IsNullOrEmpty(path)
					|| string.IsNullOrEmpty(target))
					continue;

				var a = new Avd(name!, device!, path!, target!, basedOn);

				if (a.Path.EndsWith(".avd", StringComparison.OrdinalIgnoreCase))
				{
					var avdIniFile = a.Path.Substring(0, a.Path.Length - 4) + ".ini";

					// Collect properties from the avd root level ini file
					ParseIniFile(a.Properties, avdIniFile);
				}

				// Collect properties from the config file in the avd's folder
				ParseIniFile(a.Properties, Path.Combine(a.Path, "config.ini"));

				if (!string.IsNullOrWhiteSpace(a.Name))
					r.Add(a);
			}
		}

		return r;
	}

	static Regex rxListDevices = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+OEM\s?:\s+(?<oem>[^\n]+)", RegexOptions.Singleline | RegexOptions.Compiled);

	static Regex rxIdOr = new Regex(@"^(?<num>[0-9]+)\s{0,}or\s{0,}\""(?<str>.*?)\""$", RegexOptions.Singleline | RegexOptions.Compiled);

	public IEnumerable<AvdDevice> ListDevices()
	{
		var r = new List<AvdDevice>();

		var args = new ProcessArgumentBuilder();
		args.Append("list");
		args.Append("device");

		var lines = Run(args);

		var str = string.Join("\n", lines);

		var matches = rxListDevices.Matches(str);
		if (matches != null && matches.Count > 0)
		{
			foreach (Match m in matches)
			{
				// Parse out the id further from: `18 or "pixel"`
				var idstr = m.Groups?["id"]?.Value;
				int? idnum = null;

				if (string.IsNullOrEmpty(idstr))
					continue;

				var idRx = rxIdOr.Match(idstr);

				if (idRx != null && idRx.Success && int.TryParse(idRx.Groups?["num"]?.Value, out var idInt))
				{
					idnum = idInt;
					idstr = idRx.Groups?["str"]?.Value ?? idstr;
				}

				var name = m.Groups?["name"]?.Value;
				var oem = m.Groups?["oem"]?.Value;

				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(oem))
					continue;

				var a = new AvdDevice(idstr!, name!, oem, idnum);

				if (!string.IsNullOrWhiteSpace(a.Name))
					r.Add(a);
			}
		}

		return r;
	}

	void ParseIniFile(Dictionary<string, string> properties, string iniFile)
	{
		if (!File.Exists(iniFile))
			return;

		var lines = File.ReadAllLines(iniFile);
		foreach (var line in lines)
		{
			if (!line.Contains('='))
				continue;

			var parts = line.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

			if (parts is not null && parts.Length == 2)
			{
				var key = parts[0]?.Trim()?.ToLowerInvariant();

				if (!string.IsNullOrEmpty(key))
					properties[key!] = parts[1].Trim();
			}
		}
	}

	JavaProcessRunner Start(ProcessArgumentBuilder args, bool redirectStandardInput = false)
	{
		var locator = new AvdManagerToolLocator();

		var avdToolPath = locator.FindTool(AndroidSdkHome);

		if (avdToolPath is null || avdToolPath.DirectoryName is null || !avdToolPath.Exists)
			throw new FileNotFoundException($"Could not locate {locator.ToolName}", avdToolPath?.FullName);

		var libPath = Path.GetFullPath(Path.Combine(avdToolPath.DirectoryName, "..", "lib"));
		var toolPath = Path.GetFullPath(Path.Combine(avdToolPath.DirectoryName, ".."));

		// This is the package and class that contains the main() for avdmanager
		var javaArgs = new JavaProcessArgumentBuilder("com.android.sdklib.tool.AvdManagerCli", args);

		// Set the classpath to all the .jar files we found in the lib folder
		javaArgs.AppendClassPath(Directory.GetFiles(libPath, "*.jar").Select(f => new FileInfo(f).Name));

		// This needs to be set to the working dir / classpath dir as the library looks for this system property at runtime
		javaArgs.AppendJavaToolOptions($"-Dcom.android.sdkmanager.toolsdir=\"{toolPath}\"");

		// lib folder is our working dir
		javaArgs.SetWorkingDirectory(libPath);

		var runner = new JavaProcessRunner(JdkHome, javaArgs, default, redirectStandardInput);

		return runner;
	}

	IEnumerable<string> Run(ProcessArgumentBuilder args)
	{
		var p = Start(args, false);

		var r = p.WaitForExit();

		if (r.ExitCode != 0)
		{
			throw new SdkToolFailedExitException("avdmanager", r);
		}

		return r.Output;
	}
}
