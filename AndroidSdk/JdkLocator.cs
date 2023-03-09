#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace AndroidSdk
{

	public class JdkLocator : IPathLocator
	{
		string PlatformJavaCExtension => IsWindows ? ".exe" : string.Empty;

		protected bool IsWindows
			=> RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

		protected bool IsMac
			=> RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

		public IReadOnlyList<DirectoryInfo> Locate(string? specificHome = null, params string[]? additionalPossibleDirectories)
		{
			var jdks = LocateJdk(specificHome, additionalPossibleDirectories);

			return jdks?.Select(j => j.Home)?.ToList() ?? new List<DirectoryInfo>();
		}

		public IEnumerable<JdkInfo> LocateJdk(string? specificHome = null, params string[]? additionalPossibleDirectories)
		{
			var paths = new List<JdkInfo>();

			if (specificHome != null)
			{
				SearchDirectoryForJdks(paths, specificHome, true);
			}

			if (IsWindows)
			{
				SearchDirectoryForJdks(paths,
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "Jdk"), true);

				var pfmsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft");

				try
				{
					if (Directory.Exists(pfmsDir))
					{
						var msJdkDirs = Directory.EnumerateDirectories(pfmsDir, "jdk-*", SearchOption.TopDirectoryOnly);
						foreach (var msJdkDir in msJdkDirs)
							SearchDirectoryForJdks(paths, msJdkDir, false);
					}
				}
				catch { }

				SearchDirectoryForJdks(paths,
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Jdk"), true);
			}
			else if (IsMac)
			{
				var ms11Dir = Path.Combine("/Library", "Java", "JavaVirtualMachines", "microsoft-11.jdk", "Contents", "Home");
				SearchDirectoryForJdks(paths, ms11Dir, true);

				var msDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Developer", "Xamarin", "jdk");
				SearchDirectoryForJdks(paths, msDir, true);

				// /Library/Java/JavaVirtualMachines/
				try
				{
					var javaVmDir = Path.Combine("Library", "Java", "JavaVirtualMachines");

					if (Directory.Exists(javaVmDir))
					{
						var javaVmJdkDirs = Directory.EnumerateDirectories(javaVmDir, "*.jdk", SearchOption.TopDirectoryOnly);
						foreach (var javaVmJdkDir in javaVmJdkDirs)
							SearchDirectoryForJdks(paths, javaVmDir, true);

						javaVmJdkDirs = Directory.EnumerateDirectories(javaVmDir, "jdk-*", SearchOption.TopDirectoryOnly);
						foreach (var javaVmJdkDir in javaVmJdkDirs)
							SearchDirectoryForJdks(paths, javaVmDir, true);
					}
				}
				catch
				{
				}
			}

			SearchDirectoryForJdks(paths, Environment.GetEnvironmentVariable("JAVA_HOME") ?? string.Empty, true);
			SearchDirectoryForJdks(paths, Environment.GetEnvironmentVariable("JDK_HOME") ?? string.Empty, true);

			if (additionalPossibleDirectories is not null)
			{
				foreach (var d in additionalPossibleDirectories)
				{
					SearchDirectoryForJdks(paths, d, true);
				}
			}


			var environmentPaths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();

			foreach (var envPath in environmentPaths)
			{
				string ep = envPath?.ToLowerInvariant() ?? string.Empty;

				if (!string.IsNullOrEmpty(ep) && ep.Contains("java") || ep.Contains("jdk"))
					SearchDirectoryForJdks(paths, ep, true);
			}

			return paths
				.GroupBy(i => i.JavaC.FullName)
				.Select(g => g.First());
		}

		void SearchDirectoryForJdks(IList<JdkInfo> found, string directory, bool recursive = true)
		{
			if (string.IsNullOrEmpty(directory))
				return;

			var dir = new DirectoryInfo(directory);

			if (dir.Exists)
			{
				var files = dir.EnumerateFileSystemInfos($"javac{PlatformJavaCExtension}", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

				foreach (var file in files)
				{
					if (!found.Any(f => f.JavaC.FullName.Equals(file.FullName)) && TryGetJavaJdkInfo(file.FullName, out var jdkInfo) && jdkInfo is not null)
						found.Add(jdkInfo);
				}
			}
		}

		static readonly Regex rxJavaCVersion = new Regex("[0-9\\.\\-_]+", RegexOptions.Singleline);

		bool TryGetJavaJdkInfo(string javacFilename, out JdkInfo? javaJdkInfo)
		{
			var args = new ProcessArgumentBuilder();
			args.Append("-version");

			var p = new ProcessRunner(new FileInfo(javacFilename), args);
			var result = p.WaitForExit();
			
			var m = rxJavaCVersion.Match(result.GetOutput() ?? string.Empty);

			var v = m?.Value;

			if (!string.IsNullOrEmpty(v))
			{
				javaJdkInfo = new JdkInfo(javacFilename, v);
				return true;
			}

			javaJdkInfo = null;
			return false;
		}
	}
}
