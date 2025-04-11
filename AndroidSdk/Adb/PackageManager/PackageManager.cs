using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AndroidSdk;

public partial class PackageManager : JdkTool
{
	public PackageManager(DirectoryInfo androidSdkHome, DirectoryInfo jdkHome, string? adbSerial = null)
		: base(androidSdkHome, jdkHome)
	{
		runner = new AdbRunner(this);
		AdbSerial = adbSerial;
	}

	public string? AdbSerial { get; set; }
	AdbRunner runner;

	public List<PackageListInfo> ListPackages(bool includeUninstalled = false, PackageListState showState = PackageListState.All, PackageSourceType showSource = PackageSourceType.All)
	{
		// list packages [options] filter
		// start [options] intent
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("list");
		builder.Append("packages");
		builder.Append("-f");
		builder.Append("-i");

		if (showState == PackageListState.OnlyDisabled)
			builder.Append("-d");
		else if (showState == PackageListState.OnlyEnabled)
			builder.Append("-e");

		if (showSource == PackageSourceType.OnlySystem)
			builder.Append("-s");
		else if (showSource == PackageSourceType.OnlyThirdParty)
			builder.Append("-3");

		if (includeUninstalled)
			builder.Append("-u");

		var r = runner.RunAdb(builder);

		var results = new List<PackageListInfo>();

		const string rxPackageListInfo = "^package:(?<path>.*?)=(?<package>.*?)\\s+installer=(?<installer>.*?)$";
		foreach (var line in r.StandardOutput)
		{
			var m = Regex.Match(line, rxPackageListInfo, RegexOptions.Singleline);

			var installPath = m?.Groups?["path"]?.Value;
			var packageName = m?.Groups?["package"]?.Value;
			var installer = m?.Groups?["installer"]?.Value;

			if (!string.IsNullOrEmpty(installPath) && !string.IsNullOrEmpty(packageName))
				results.Add(new PackageListInfo(new FileInfo(installPath), packageName!, installer));
		}
		return results;
	}

	public List<string> ListPermissionGroups()
	{
		// list packages [options] filter
		// start [options] intent
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("list");
		builder.Append("permission-groups");

		var r = runner.RunAdb(builder);

		var results = new List<string>();

		const string rxPackageListInfo = "^permission group:(?<group>.*?)$";
		foreach (var line in r.StandardOutput)
		{
			var m = Regex.Match(line, rxPackageListInfo, RegexOptions.Singleline);

			var pg = m?.Groups?["group"]?.Value;

			if (!string.IsNullOrEmpty(pg))
				results.Add(pg!);
		}
		return results;
	}

	public List<PermissionGroupInfo> ListPermissions(bool onlyDangerous = false, bool onlyUserVisible = false)
	{
		// list packages [options] filter
		// start [options] intent
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("list");
		builder.Append("permissions");
		builder.Append("-g");
		builder.Append("-f");

		if (onlyDangerous)
			builder.Append("-d");
		if (onlyUserVisible)
			builder.Append("-u");

		var r = runner.RunAdb(builder);

		var results = new List<PermissionGroupInfo>();

		PermissionGroupInfo? currentGroup = null;
		PermissionInfo? currentPerm = null;

		foreach (var line in r.StandardOutput)
		{
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith("All Permissions:", StringComparison.OrdinalIgnoreCase))
				continue;

			if (line.StartsWith("+ group:"))
			{
				if (currentGroup != null && currentPerm != null)
				{
					currentGroup.Permissions.Add(currentPerm);
					currentPerm = null;
				}

				if (currentGroup is not null)
					results.Add(currentGroup);

				currentGroup = new PermissionGroupInfo(line.Substring(8));
			}
			else if (line.StartsWith("  package:"))
			{
				if (currentGroup is not null)
					currentGroup.PackageName = line.Substring(10);
			}
			else if (line.StartsWith("  label:"))
			{
				if (currentGroup is not null)
					currentGroup.Label = line.Substring(8);
			}
			else if (line.StartsWith("  description:"))
			{
				if (currentGroup is not null)
					currentGroup.Label = line.Substring(14);
			}
			else if (line.StartsWith("  + permission:"))
			{
				if (currentPerm != null && currentGroup != null)
					currentGroup.Permissions.Add(currentPerm);

				currentPerm = new PermissionInfo(line.Substring(15));
			}
			else if (line.StartsWith("    package:"))
			{
				if (currentPerm is not null)
					currentPerm.PackageName = line.Substring(12);
			}
			else if (line.StartsWith("    label:"))
			{
				if (currentPerm is not null)
					currentPerm.Label = line.Substring(10);
			}
			else if (line.StartsWith("    description:"))
			{
				if (currentPerm is not null)
					currentPerm.Description = line.Substring(16);
			}
			else if (line.StartsWith("    protectionLevel:"))
			{
				var plraw = line.Substring(20);
				if (currentPerm is not null)
					currentPerm.ProtectionLevels.AddRange(plraw.Split('|'));
			}
		}

		if (currentPerm != null && currentGroup != null)
			currentGroup.Permissions.Add(currentPerm);

		if (currentGroup != null)
			results.Add(currentGroup);

		return results;
	}

	public List<string> ListFeatures()
	{
		// list packages [options] filter
		// start [options] intent
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("list");
		builder.Append("features");

		var r = runner.RunAdb(builder);

		var results = new List<string>();

		const string rxPackageListInfo = "^feature:(?<feature>.*?)$";
		foreach (var line in r.StandardOutput)
		{
			var m = Regex.Match(line, rxPackageListInfo, RegexOptions.Singleline);

			var ft = m?.Groups?["feature"]?.Value;

			if (!string.IsNullOrEmpty(ft))
				results.Add(ft!);
		}
		return results;
	}

	public List<string> ListLibraries()
	{
		// list packages [options] filter
		// start [options] intent
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("list");
		builder.Append("libraries");

		var r = runner.RunAdb(builder);

		var results = new List<string>();

		const string rxPackageListInfo = "^library:(?<lib>.*?)$";
		foreach (var line in r.StandardOutput)
		{
			var m = Regex.Match(line, rxPackageListInfo, RegexOptions.Singleline);

			var lib = m?.Groups?["lib"]?.Value;

			if (!string.IsNullOrEmpty(lib))
				results.Add(lib!);
		}
		return results;
	}


	public FileInfo? PathToPackage(string packageName)
	{
		// list packages [options] filter
		// start [options] intent
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("path");
		builder.Append(packageName);

		var r = runner.RunAdb(builder);

		const string rxPackageListInfo = "^package:(?<path>.*?)$";
		foreach (var line in r.StandardOutput)
		{
			var m = Regex.Match(line, rxPackageListInfo, RegexOptions.Singleline);

			var path = m?.Groups?["path"]?.Value;

			if (string.IsNullOrEmpty(path))
				continue;

			return new FileInfo(path);
		}

		return null;
	}


	public void Install(FileInfo pathOnDevice,
								bool forwardLock = false,
								bool reinstall = false,
								bool allowTestApks = false,
								string? installerPackageName = null,
								bool installOnSharedStorage = false,
								bool installOnInternalSystemMemory = false,
								bool allowVersionDowngrade = false,
								bool grantAllManifestPermissions = false)
	{
		// install[options] path
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("install");

		if (forwardLock)
			builder.Append("-l");
		if (reinstall)
			builder.Append("-r");
		if (allowTestApks)
			builder.Append("-t");
		if (installOnSharedStorage)
			builder.Append("-s");
		if (installOnInternalSystemMemory)
			builder.Append("-f");
		if (allowVersionDowngrade)
			builder.Append("-d");
		if (grantAllManifestPermissions)
			builder.Append("-g");

		builder.AppendQuoted(pathOnDevice.FullName);

		runner.RunAdb(builder);
	}


	//public void Uninstall(string packageName, bool keepDataAndCache = false)
	//{
	//	// uninstall [options] package
	//	var builder = new ProcessArgumentBuilder();

	//	runner.AddSerial(Settings.Serial, builder);

	//	builder.Append("shell");
	//	builder.Append("pm");

	//	builder.Append("uninstall");

	//	if (keepDataAndCache)
	//		builder.Append("-k");

	//	builder.Append(packageName);

	//	var output = new List<string>();
	//	runner.RunAdb(Settings, builder, out output);
	//}

	public void Clear(string packageName)
	{
		// clear package
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("clear");
		builder.Append(packageName);

		runner.RunAdb(builder);
	}

	public void Enable(string packageOrComponent)
	{
		// clear package
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("enable");
		builder.Append(packageOrComponent);

		runner.RunAdb(builder);
	}

	public void Disable(string packageOrComponent)
	{
		// clear package
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("disable");
		builder.Append(packageOrComponent);

		runner.RunAdb(builder);
	}

	public void DisableUser(string packageOrComponent, string? forUser = null)
	{
		// disable-user [options] package_or_component
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("disable");

		if (!string.IsNullOrEmpty(forUser))
		{
			builder.Append("--user");
			builder.Append(forUser!);
		}

		builder.Append(packageOrComponent);

		runner.RunAdb(builder);
	}

	public void Grant(string packageName, string permission)
	{
		// grant package_name permission
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("grant");
		builder.Append(packageName);
		builder.Append(permission);

		runner.RunAdb(builder);
	}

	public void Revoke(string packageName, string permission)
	{
		// revoke package_name permission
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("revoke");
		builder.Append(packageName);
		builder.Append(permission);

		runner.RunAdb(builder);
	}

	public void SetInstallLocation(PackageInstallLocation location)
	{
		// set-install-location location
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("set-install-location");
		builder.Append(((int)location).ToString());

		runner.RunAdb(builder);
	}

	public PackageInstallLocation GetInstallLocation()
	{
		// set-install-location location
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("get-install-location");

		var r = runner.RunAdb(builder);

		var o = string.Join(Environment.NewLine, r.StandardOutput);

		if (o.Contains("[internal]"))
			return PackageInstallLocation.Internal;
		if (o.Contains("[external]"))
			return PackageInstallLocation.External;

		return PackageInstallLocation.Auto;
	}


	public void SetPermissionEnforced(string permission, bool enforced)
	{
		// set-permission-enforced permission [true|false]
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("set-permission-enforced");
		builder.Append(permission);
		builder.Append(enforced ? "true" : "false");

		runner.RunAdb(builder);
	}

	public void TrimCaches(string desiredFreeSpace)
	{
		// trim-caches desired_free_space
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("trim-caches");
		builder.Append(desiredFreeSpace);

		runner.RunAdb(builder);
	}

	public void CreateUser(string userName)
	{
		// create-user user_name
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("create-user");
		builder.Append(userName);

		runner.RunAdb(builder);
	}

	public void RemoveUser(string userId)
	{
		// remove-user user_id
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("remove-user");
		builder.Append(userId);

		runner.RunAdb(builder);
	}

	public int GetMaxUsers()
	{
		// get-max-users
		var builder = new ProcessArgumentBuilder();

		runner.AddSerial(AdbSerial, builder);

		builder.Append("shell");
		builder.Append("pm");

		builder.Append("get-max-users");

		var r = runner.RunAdb(builder);

		var o = r.StandardOutput.FirstOrDefault() ?? string.Empty;

		if (o.StartsWith("Maximum supported users:", StringComparison.OrdinalIgnoreCase))
		{
			int result = -1;
			var num = o.Substring(24).Trim();
			if (int.TryParse(num, out result))
				return result;
		}

		return -1;
	}
}
