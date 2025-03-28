using AndroidRepository;
using AndroidRepository.AddonsList_6;
using AndroidRepository.SitesCommon_1;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AndroidSdk.Tests.Repository;

public class RepositoryManifestTests
{
    [Fact]
    public async Task License_GetLicenseFileHash_ValidatesAndroidSdkLicense()
    {
		var mgr = new AndroidRepository.RepositoryManager();

		var r = await mgr.LoadUrlAsync<AndroidRepository.Repository2_3.SdkRepository>("https://dl.google.com/android/repository/repository2-3.xml");


        // Find the android-sdk-license and verify its hash
        var sdkLicense = r.License.FirstOrDefault(l => l.Id == "android-sdk-license");
        Assert.NotNull(sdkLicense);
        var hash = sdkLicense.GetLicenseFileHash();
        Assert.Equal("24333f8a63b6825ea9c5514f83c2829b004d1fee", hash);

        // Find the android-sdk-preview-license and verify its hash
        var sdkLicense2 = r.License.FirstOrDefault(l => l.Id == "android-sdk-preview-license");
        Assert.NotNull(sdkLicense2);
        var hash2 = sdkLicense2.GetLicenseFileHash();
        Assert.Equal("84831b9409646a918e30573bab4c9c91346d8abd", hash2);
    }

	[Fact]
	public async Task Get_Latest_Cmdline_Tools()
	{
		var mgr = new AndroidRepository.RepositoryManager();

		var r = await mgr.LoadUrlAsync<AndroidRepository.Repository2_3.SdkRepository>("https://dl.google.com/android/repository/repository2-3.xml");

		var channel = r.Channel.Where(c => c.Value == ChannelTypes.Stable).FirstOrDefault();

		// Find the android-sdk-license and verify its hash  
		var cmdlineTools = r.RemotePackage.Where(p => p.ChannelRef.Ref == channel.Id
			&& !p.Revision.PreviewSpecified
			&& p.UsesLicense.Ref == "android-sdk-license"
			&& p.Path.StartsWith("cmdline-tools;"))
			.OrderByDescending(p => p.Revision.Major)
			.ThenByDescending(p => p.Revision.Micro)
			.ThenByDescending(p => p.Revision.Preview)
			.FirstOrDefault();

		Assert.NotNull(cmdlineTools);
		Assert.True(cmdlineTools.Revision.Major >= 17);
	}


	[Fact]
	public async Task Get_AddOnsList_Async()
	{
		var mgr = new AndroidRepository.RepositoryManager();

		var r = await mgr.GetAddonsListAsync();

		Assert.True(r.Site.Count >= 15);
	}


	[Fact]
	public async Task Get_AndroidSystemImages_Async()
	{
		var mgr = new AndroidRepository.RepositoryManager();

		var r = await mgr.GetAddonsListAsync();

		var si = r.Site.FirstOrDefault(s => s is SysImgSiteType && s.DisplayName == "Android System Images") as SysImgSiteType;

		Assert.NotNull(si);

		var androidSysImgs = await si.LoadAsync();

		Assert.NotNull(androidSysImgs);
	}

	[Fact]
	public async Task Get_GoogleIncAddons_Async()
	{
		var mgr = new AndroidRepository.RepositoryManager();

		var r = await mgr.GetAddonsListAsync();

		var si = r.Site.FirstOrDefault(s => s is AddonSiteType && s.DisplayName == "Google Inc.") as AddonSiteType;

		Assert.NotNull(si);

		var googleIncAddons = await si.LoadAsync();

		Assert.NotNull(googleIncAddons);
	}


	[Fact]
	public async Task Can_Parse_All_Addons_Lists()
	{
		var mgr = new AndroidRepository.RepositoryManager();

		var r = await mgr.GetAddonsListAsync();

		foreach (var s in r.Site)
		{
			if (s is AddonSiteType addonSite)
			{
				var addons = await addonSite.LoadAsync();
				Assert.NotNull(addons);

				if (addons.RemotePackageSpecified)
					Assert.NotEmpty(addons.RemotePackage);
			}
			else if (s is SysImgSiteType sysImgSite)
			{
				var sysImgs = await sysImgSite.LoadAsync();
				Assert.NotNull(sysImgs);

				if (sysImgs.RemotePackageSpecified)
					Assert.NotEmpty(sysImgs.RemotePackage);
			}
		}
	}
}