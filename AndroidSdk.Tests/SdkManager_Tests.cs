using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

[Collection(AndroidSdkManagerCollection.Name)]
public class SdkManager_Tests : AndroidSdkManagerTestsBase
{
	public SdkManager_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture)
		: base(outputHelper, fixture)
	{
	}

	[Fact]
	public void List()
	{
		var list = Sdk.SdkManager.List();

		Assert.NotEmpty(list.AvailablePackages);

		Assert.NotEmpty(list.InstalledPackages);

		foreach (var a in list.AvailablePackages)
			OutputHelper.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");

		foreach (var a in list.InstalledPackages)
			OutputHelper.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");
	}

	[Fact]
	public void GetLicenses()
	{
		var list = Sdk.SdkManager.GetLicenses();

		Assert.NotNull(list);
	}

	[Fact]
	public void GetAcceptedLicenseIds()
	{
		var list = Sdk.SdkManager.GetAcceptedLicenseIds();

		Assert.NotNull(list);
	}

	[Fact]
	public void Install()
	{
		const string PackageToInstall = "extras;google;auto";

		var ok = Sdk.SdkManager.Install(PackageToInstall);

		Assert.True(ok);

		var list = Sdk.SdkManager.List();

		Assert.Contains(PackageToInstall, list.InstalledPackages.Select(ip => ip.Path));
	}

	[Fact]
	public void InstallPlatform()
	{
		const string PackageToInstall = "platforms;android-33";

		var ok = Sdk.SdkManager.Install(PackageToInstall);

		Assert.True(ok);

		var list = Sdk.SdkManager.List();

		Assert.Contains(PackageToInstall, list.InstalledPackages.Select(ip => ip.Path));
	}

	[Fact]
	public void Uninstall()
	{
		const string PackageToUninstall = "extras;google;auto";

		var ok = Sdk.SdkManager.Uninstall(PackageToUninstall);

		Assert.True(ok);

		var list = Sdk.SdkManager.List();

		Assert.DoesNotContain(PackageToUninstall, list.InstalledPackages.Select(ip => ip.Path));
	}

	[Fact]
	public void AcceptLicense()
	{
		Sdk.SdkManager.AcceptLicenses();

		var list = Sdk.SdkManager.List();

		Assert.NotNull(list.InstalledPackages);
	}

	[Fact]
	public void ProcessRunner_Logs()
	{
		var tmp = Path.GetTempFileName();
		Environment.SetEnvironmentVariable("ANDROID_TOOL_PROCESS_RUNNER_LOG_PATH", tmp);

		var list = Sdk.SdkManager.List();

		var logText = File.ReadAllText(tmp);

		Assert.NotEmpty(logText);
	}
}
