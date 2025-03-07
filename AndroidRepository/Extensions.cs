using AndroidRepository.Addon2_3;
using AndroidRepository.AddonsList_6;
using AndroidRepository.Common_2;
using AndroidRepository.SitesCommon_1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace AndroidRepository;

public static class ChannelTypes
{
	public const string Stable = "stable";
	public const string Beta = "beta";
	public const string Dev = "dev";
	public const string Canary = "canary";
	public static string GetBaseUrl(this RepositoryType repository)
	{
		if (repository == null)
			throw new ArgumentNullException(nameof(repository));

		if (string.IsNullOrEmpty(repository.Url))
			throw new ArgumentException("Repository URL cannot be null or empty.", nameof(repository));

		var uri = new Uri(repository.Url);
		var baseUrl = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
		return baseUrl;
	}

}

public static class Extensions
{
	public static string GetLicenseFileHash(this Common_2.LicenseType licenseType)
	{
		var text = licenseType.Value;

		if (string.IsNullOrEmpty(text))
			return string.Empty;

		// The SDK uses a specific treatment of the license text to generate the hash.  Code was derived from:
		// https://android.googlesource.com/platform/tools/base/+/HEAD/repository/src/main/java/com/android/repository/impl/meta/TrimStringAdapter.java
		var str1 = Regex.Replace(text, @"(?<=\s)[ \t]*", ""); // remove spaces and tabs preceded by space, tab, or newline.
		var str2 = Regex.Replace(str1, @"(?<!\n)\n(?!\n)", " "); // replace lone newlines with space
		var str3 = Regex.Replace(str2, @" +", " "); // remove duplicate spaces possibly caused by previous step
		var str4 = str3.Trim(); // remove leading or trailing spaces

		// var result = Regex.Replace(
		//     Regex.Replace(
		//         Regex.Replace(Text, @"(?<=\s)[ \t]*", ""), // remove spaces and tabs preceded by space, tab, or newline.
		//         @"(?<!\n)\n(?!\n)", " "),          // replace lone newlines with space
		//         @" +", " ")                         // remove duplicate spaces possibly caused
		//                                            // by previous step
		//     .Trim();                               // remove leading or trailing spaces

		using (var sha1 = SHA1.Create())
		{
			var bytes = Encoding.UTF8.GetBytes(str4);
			var hash = sha1.ComputeHash(bytes);
			return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
		}
	}

	public static string GetFullUrl(this SitesCommon_1.SiteType site, string baseUrl = RepositoryManager.GoogleAndroidRepositoryUrlBase)
	{
		if (site == null)
			throw new ArgumentNullException(nameof(site));
		return baseUrl.TrimEnd('/') + "/" + site.Url.TrimStart('/');
	}

	public static string GetFullUrl(this ArchiveType archive, string baseUrl = RepositoryManager.GoogleAndroidRepositoryUrlBase)
	{
		if (archive == null)
			throw new ArgumentNullException(nameof(archive));

		var basePath = GetBasePath(baseUrl);
		return new Uri(new Uri(basePath), archive.Complete.Url).ToString();
	}

	public static string GetFullUrl(this ArchiveType archive, RepositoryType repository)
	{
		if (archive == null)
			throw new ArgumentNullException(nameof(archive));

		if (repository == null)
			throw new ArgumentNullException(nameof(repository));
		
		var basePath = GetBasePath(repository.Url);
		return new Uri(new Uri(basePath), archive.Complete.Url).ToString();
	}

	static string GetBasePath(string url)
	{
		Uri uri = new Uri(url);
		string basePath = uri.GetLeftPart(UriPartial.Path);

		// Remove any filename at the end of the path
		int lastSlashIndex = basePath.LastIndexOf('/');
		if (lastSlashIndex > uri.GetLeftPart(UriPartial.Authority).Length)
		{
			basePath = basePath.Substring(0, lastSlashIndex + 1);
		}

		return basePath;
	}

	public static Task<TSiteType> LoadAsync<TSiteType>(this SitesCommon_1.SiteType site, string baseUrl = RepositoryManager.GoogleAndroidRepositoryUrlBase)
		where TSiteType : SiteType
	{
		if (site == null)
			throw new ArgumentNullException(nameof(site));
		var url = site.GetFullUrl(baseUrl);
		var mgr = new RepositoryManager();
		return mgr.LoadUrlAsync<TSiteType>(url);
	}


	public static Task<TSdkSysImg> LoadAsync<TSdkSysImg>(this SysImgSiteType site, string baseUrl = RepositoryManager.GoogleAndroidRepositoryUrlBase)
		where TSdkSysImg : AndroidRepository.Common_2.RepositoryType
	{
		if (site == null)
			throw new ArgumentNullException(nameof(site));
		var url = site.GetFullUrl(baseUrl);
		var mgr = new RepositoryManager();
		return mgr.LoadUrlAsync<TSdkSysImg>(url);
	}

	public static Task<SysImg2_4.SdkSysImg> LoadAsync(this SysImgSiteType site, string baseUrl = RepositoryManager.GoogleAndroidRepositoryUrlBase)
		=> LoadAsync<SysImg2_4.SdkSysImg>(site, baseUrl);



	public static Task<TAddonImg> LoadAsync<TAddonImg>(this AddonSiteType site, string baseUrl = RepositoryManager.GoogleAndroidRepositoryUrlBase)
		where TAddonImg : AndroidRepository.Common_2.RepositoryType
	{
		if (site == null)
			throw new ArgumentNullException(nameof(site));
		var url = site.GetFullUrl(baseUrl);
		var mgr = new RepositoryManager();
		return mgr.LoadUrlAsync<TAddonImg>(url);
	}

	public static Task<SdkAddon> LoadAsync(this AddonSiteType site, string baseUrl = RepositoryManager.GoogleAndroidRepositoryUrlBase)
		=> LoadAsync<SdkAddon>(site, baseUrl);


	public static ArchiveType GetHostArchive(this IEnumerable<Common_2.ArchiveType> archives, string? hostOS = null, string? hostArch = null)
	{
		hostOS ??= GetHostOS();
		hostArch ??= GetHostArch();

		var osArchives = new List<ArchiveType>();

		foreach (var a in archives)
		{
			if (string.IsNullOrEmpty(a.HostOs) || a.HostOs.Equals(hostOS, StringComparison.OrdinalIgnoreCase))
			{
				osArchives.Add(a);
			}
		}

		var archArchives = new List<ArchiveType>();

		foreach (var a in osArchives)
		{
			if (string.IsNullOrEmpty(a.HostArch) || a.HostArch.Equals(GetHostArch(), StringComparison.OrdinalIgnoreCase))
			{
				archArchives.Add(a);
			}
		}

		return archArchives.OrderBy(a => !string.IsNullOrEmpty(a.HostOs)).ThenBy(a => !string.IsNullOrEmpty(a.HostArch)).FirstOrDefault()
			?? osArchives.FirstOrDefault() ?? archives.First();
	}

	public static string GetHostOS()
	{
		if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
		{
			return "windows";
		}
		else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
		{
			return "linux";
		}
		else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
		{
			return "macosx";
		}

		return "win";
	}

	public static string GetHostArch()
	{
		if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64)
		{
			return "x64";
		}
		else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X86)
		{
			return "x86";
		}
		else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm)
		{
			return "aarch";
		}
		else if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
		{
			return "aarch64";
		}
		return "aarch64";
	}
}
