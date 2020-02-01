using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace Android.Tools
{
	public partial class AvdManager
	{
		public AvdManager(DirectoryInfo androidSdkHome)
		{
			AndroidSdkHome = androidSdkHome;
		}

		public AvdManager(string androidSdkHome)
			: this(new DirectoryInfo(androidSdkHome))
		{ }

		public DirectoryInfo AndroidSdkHome { get; set; }

		public void AvdCreate(string name, string sdkId, string device, string sdCardPathOrSize = null, bool force = false, string avdPath = null)
		{
			var args = new List<string> {
				"create", "avd", "-n", name, "-k", $"\"{sdkId}\""
			};

			if (!string.IsNullOrEmpty(device))
			{
				args.Add("--device");
				args.Add($"\"{device}\"");
			}

			if (!string.IsNullOrEmpty(sdCardPathOrSize))
			{
				args.Add("-c");
				args.Add($"\"{sdCardPathOrSize}\"");
			}

			if (force)
				args.Add("--force");

			if (!string.IsNullOrEmpty(avdPath))
			{
				args.Add("-p");
				args.Add($"\"{avdPath}\"");
			}

			run(args.ToArray());
		}

		public void AvdDelete(string name)
		{
			run("delete", "avd", "-n", name);
		}

		public void AvdMove(string name, string path = null, string newName = null)
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

		public IEnumerable<AvdTarget> AvdListTargets()
		{
			foreach (var line in run("list", "target", "-c"))
				yield return new AvdTarget { Id = line.Trim() };
		}

		public IEnumerable<Avd> AvdListAvds()
		{
			foreach (var line in run("list", "avd", "-c"))
				yield return new Avd { Name = line.Trim() };
		}

		public IEnumerable<AvdDevice> AvdListDevices()
		{
			foreach (var line in run("list", "device", "-c"))
				yield return new AvdDevice { Name = line.Trim() };
		}

		IEnumerable<string> run(params string[] args)
		{
			var adbManager = AndroidSdk.FindAvdManager(AndroidSdkHome);
			if (adbManager == null || !File.Exists(adbManager.FullName))
				throw new FileNotFoundException("Could not find avdmanager", adbManager?.FullName);

			var builder = new ProcessArgumentBuilder();

			foreach (var arg in args)
				builder.Append(arg);

			var p = new ProcessRunner(adbManager, builder);

			var r = p.WaitForExit();

			return r.StandardOutput;
		}

		public void Acquire()
		{
			var sdkManager = new SdkManager(AndroidSdkHome);

			sdkManager.Acquire("emulator");
		}
	}
}
