using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AndroidSdk
{
	public partial class AvdManager : SdkTool
	{
		public AvdManager()
			: this((DirectoryInfo)null)
		{ }

		public AvdManager(DirectoryInfo androidSdkHome)
			: base(androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
		}

		public AvdManager(string androidSdkHome)
			: this(new DirectoryInfo(androidSdkHome))
		{ }

		JdkInfo jdk = null;

		public override FileInfo FindToolPath(DirectoryInfo androidSdkHome)
		{
			var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			var ext = isWindows ? ".bat" : string.Empty;

			var likelyPathSegments = new List<string[]>();

			var cmdlineToolsPath = new DirectoryInfo(Path.Combine(androidSdkHome.FullName, "cmdline-tools"));

			if (cmdlineToolsPath.Exists)
			{
				foreach (var dir in cmdlineToolsPath.GetDirectories())
				{
					var toolPath = new FileInfo(Path.Combine(dir.FullName, "bin", "avdmanager" + ext));
					if (toolPath.Exists)
						likelyPathSegments.Insert(0, new[] { "cmdline-tools", dir.Name, "bin" });
				}
			}

			likelyPathSegments.Add(new[] { "tools", "bin" });

			foreach (var pathSeg in likelyPathSegments)
			{
				var tool = FindTool(androidSdkHome, toolName: "avdmanager", windowsExtension: ".bat", pathSeg);
				if (tool != null)
					return tool;
			}

			return null;
		}

		internal override string SdkPackageId => "emulator";

		public void Create(string name, string sdkId, string device = null, string path = null, bool force = false)
		{
			var args = new List<string> {
				"create", "avd", "-n", name, "-k", $"\"{sdkId}\""
			};

			if (!string.IsNullOrEmpty(device))
			{
				args.Add("--device");
				args.Add($"\"{device}\"");
			}

			if (!string.IsNullOrEmpty(path))
			{
				args.Add("-c");
				args.Add($"\"{path}\"");
			}

			if (force)
				args.Add("--force");

			if (!string.IsNullOrEmpty(path))
			{
				args.Add("-p");
				args.Add($"\"{path}\"");
			}

			run(args.ToArray());
		}

		public void Delete(string name)
		{
			run("delete", "avd", "-n", name);
		}

		public void Move(string name, string path = null, string newName = null)
		{
			var args = new List<string> {
				"move", "avd", "-n", name
			};

			if (!string.IsNullOrEmpty(path))
			{
				args.Add("-p");
				args.Add(path);
			}

			if (!string.IsNullOrEmpty(newName))
			{
				args.Add("-r");
				args.Add(newName);
			}

			run(args.ToArray());
		}

		static Regex rxListTargets = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+Type\s?:\s+(?<type>[^\n]+)\s+API level\s?:\s+(?<api>[^\n]+)\s+Revision\s?:\s+(?<revision>[^\n]+)", RegexOptions.Multiline | RegexOptions.Compiled);

		public IEnumerable<AvdTarget> ListTargets()
		{
			var r = new List<AvdTarget>();

			var lines = run("list", "target");

			var str = string.Join("\n", lines);

			var matches = rxListTargets.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var a = new AvdTarget
					{
						Name = m.Groups?["name"]?.Value,
						Id = m.Groups?["id"]?.Value,
						Type = m.Groups?["type"]?.Value
					};

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

		static Regex rxListAvds = new Regex(@"\s+Name:\s+(?<name>[^\n]+)\s+Device:\s+(?<device>[^\n]+)\s+Path:\s+(?<path>[^\n]+)\s+Target:\s+(?<target>[^\n]+)\s+Based on:\s+(?<basedon>[^\n]+)", RegexOptions.Compiled | RegexOptions.Multiline);
		public IEnumerable<Avd> ListAvds()
		{
			var r = new List<Avd>();

			var lines = run("list", "avd");

			var str = string.Join("\n", lines);

			var matches = rxListAvds.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var a = new Avd
					{
						Name = m.Groups?["name"]?.Value,
						Device = m.Groups?["device"]?.Value,
						Path = m.Groups?["path"]?.Value,
						Target = m.Groups?["target"]?.Value,
						BasedOn = m.Groups?["basedon"]?.Value
					};

					if (!string.IsNullOrWhiteSpace(a.Name))
						r.Add(a);
				}
			}

			return r;
		}

		static Regex rxListDevices = new Regex(@"id:\s+(?<id>[^\n]+)\s+Name:\s+(?<name>[^\n]+)\s+OEM\s?:\s+(?<oem>[^\n]+)", RegexOptions.Singleline | RegexOptions.Compiled);

		public IEnumerable<AvdDevice> ListDevices()
		{
			var r = new List<AvdDevice>();

			var lines = run("list", "device");

			var str = string.Join("\n", lines);

			var matches = rxListDevices.Matches(str);
			if (matches != null && matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					var a = new AvdDevice
					{
						Name = m.Groups?["name"]?.Value,
						Id = m.Groups?["id"]?.Value,
						Oem = m.Groups?["oem"]?.Value
					};

					if (!string.IsNullOrWhiteSpace(a.Name))
						r.Add(a);
				}
			}

			return r;
		}

		IEnumerable<string> run(params string[] args)
		{
			if (jdk == null)
				jdk = Jdks.FirstOrDefault();

			var adbManager = FindToolPath(AndroidSdkHome);
			var java = jdk.Java;

			var libPath = Path.GetFullPath(Path.Combine(adbManager.DirectoryName, "..", "lib"));
			var toolPath = Path.GetFullPath(Path.Combine(adbManager.DirectoryName, ".."));

			var cpSeparator = IsWindows ? ";" : ":";

			// Get all the .jars in the tools\lib folder to use as classpath
			//var classPath = "avdmanager-classpath.jar";
			var classPath = string.Join(cpSeparator, Directory.GetFiles(libPath, "*.jar").Select(f => new FileInfo(f).Name));

			var proc = new Process();
			// This is the package and class that contains the main() for avdmanager
			proc.StartInfo.Arguments = "com.android.sdklib.tool.AvdManagerCli " + string.Join(" ", args);
			// This needs to be set to the working dir / classpath dir as the library looks for this system property at runtime
			proc.StartInfo.Environment["JAVA_TOOL_OPTIONS"] = $"-Dcom.android.sdkmanager.toolsdir=\"{toolPath}\"";
			// Set the classpath to all the .jar files we found in the lib folder
			proc.StartInfo.Environment["CLASSPATH"] = classPath;

			// Java.exe
			proc.StartInfo.FileName = java.FullName;

			// lib folder is our working dir
			proc.StartInfo.WorkingDirectory = libPath;

			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;

			var output = new List<string>();

			proc.OutputDataReceived += (s, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
					output.Add(e.Data);
			};
			proc.ErrorDataReceived += (s, e) =>
			{
				if (!string.IsNullOrEmpty(e.Data))
					output.Add(e.Data);
			};

			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			proc.WaitForExit();

			return output;
		}
	}
}
