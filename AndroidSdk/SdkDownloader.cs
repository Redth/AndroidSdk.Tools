using AndroidRepository;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace AndroidSdk
{
	public class SdkDownloader
	{
		readonly Regex rxPkgRevision = new Regex(@"^Pkg\.Revision=(?<rev>.+)$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Downloads the Android SDK
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="destinationDirectory">Destination directory, or ./tools/androidsdk if none is specified.</param>
		/// <param name="specificVersion">Specific version, or latest if none is specified.</param>
		public async Task DownloadAsync(DirectoryInfo destinationDirectory, (int? major, int? minor)? specificVersion = null, bool allowPreview = false, string? hostOs = null, string? hostArch = null, Action<int>? progressHandler = null)
		{
			if (destinationDirectory == null)
				throw new DirectoryNotFoundException("Android SDK Directory was not specified.");

			if (!destinationDirectory.Exists)
				destinationDirectory.Create();

			

			var repositoryManager = new AndroidRepository.RepositoryManager();
			var sdkRepo = await repositoryManager.GetSdkRepositoryAsync();
			var stableChannel = sdkRepo.Channel.FirstOrDefault(c => c.Value == ChannelTypes.Stable);

			// Find all the cmdline-tools packages in the feed, scope to non-preview versions if specified
			var cmdlineTools = sdkRepo.RemotePackage.Where(p => p.Path.StartsWith("cmdline-tools;")

				// Previews allowed?	
				&& (allowPreview || !p.Revision.PreviewSpecified)
				//// If not previews allowed, check for stable channel
				&& (allowPreview || (stableChannel is not null && p.ChannelRef.Ref == stableChannel.Id)));

			//  If a specific version is specified, filter the cmdline-tools packages to match
			if (specificVersion != null)
			{
				cmdlineTools = cmdlineTools.Where(p =>
					(!specificVersion.Value.major.HasValue || specificVersion.Value.major.Value == p.Revision.Major)
					&& (!specificVersion.Value.minor.HasValue || specificVersion.Value.minor.Value == p.Revision.Micro));
			}

			var bestMatch = cmdlineTools
				// Sort by major, minor, and preview version
				.OrderByDescending(p => p.Revision.Major)
				.ThenByDescending(p => p.Revision.Micro)
				.ThenByDescending(p => p.Revision.Preview)
				.FirstOrDefault();

			var bestMatchVersion = $"{bestMatch.Revision.Major}.{bestMatch.Revision.Micro}";

			// Get the host arch archive
			var archive = bestMatch.Archives.GetHostArchive(hostOs, hostArch);
			var sdkUrl = archive.GetFullUrl(sdkRepo);


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
				var toolsVersion = bestMatchVersion;
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
							if (!string.IsNullOrWhiteSpace(matched))
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
	}
}
