#nullable enable
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class SdkManager_NotInstalled_Tests : TestsBase
{
	readonly string tempSdkDir;

	public SdkManager_NotInstalled_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
		// Create a minimal SDK without cmdline-tools
		tempSdkDir = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "NoCmdlineTools", Guid.NewGuid().ToString());
		SetupMinimalSdk(tempSdkDir);
	}

	public override void Dispose()
	{
		try { Directory.Delete(tempSdkDir, true); } catch { }
		base.Dispose();
	}

	static void SetupMinimalSdk(string sdkDir)
	{
		// Create platform-tools with package.xml
		var ptDir = Path.Combine(sdkDir, "platform-tools");
		Directory.CreateDirectory(ptDir);
		File.WriteAllText(Path.Combine(ptDir, "package.xml"), @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ns2:repository xmlns:ns2=""http://schemas.android.com/repository/android/common/02"">
    <localPackage path=""platform-tools"" obsolete=""false"">
        <revision><major>36</major><minor>0</minor><micro>2</micro></revision>
        <display-name>Android SDK Platform-Tools</display-name>
    </localPackage>
</ns2:repository>");

		// Create emulator with package.xml
		var emuDir = Path.Combine(sdkDir, "emulator");
		Directory.CreateDirectory(emuDir);
		File.WriteAllText(Path.Combine(emuDir, "package.xml"), @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ns2:repository xmlns:ns2=""http://schemas.android.com/repository/android/common/02"">
    <localPackage path=""emulator"">
        <revision><major>36</major><minor>3</minor><micro>10</micro></revision>
        <display-name>Android Emulator</display-name>
    </localPackage>
</ns2:repository>");

		// Create build-tools with package.xml
		var btDir = Path.Combine(sdkDir, "build-tools", "36.1.0");
		Directory.CreateDirectory(btDir);
		File.WriteAllText(Path.Combine(btDir, "package.xml"), @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ns2:repository xmlns:ns2=""http://schemas.android.com/repository/android/common/02"">
    <localPackage path=""build-tools;36.1.0"">
        <revision><major>36</major><minor>1</minor><micro>0</micro></revision>
        <display-name>Android SDK Build-Tools 36.1</display-name>
    </localPackage>
</ns2:repository>");

		// No cmdline-tools!
	}

	[Fact]
	public void FindToolPath_ReturnsNull_WhenCmdlineToolsMissing()
	{
		var m = new SdkManager(tempSdkDir);
		var tool = m.FindToolPath(m.AndroidSdkHome);
		Assert.Null(tool);
	}

	[Fact]
	public void List_FallsBackToScanner_WhenCmdlineToolsMissing()
	{
		var m = new SdkManager(tempSdkDir);
		var list = m.List();

		Assert.NotNull(list);
		Assert.Empty(list.AvailablePackages);
		Assert.NotEmpty(list.InstalledPackages);

		var paths = list.InstalledPackages.Select(p => p.Path).ToList();
		Assert.Contains("platform-tools", paths);
		Assert.Contains("emulator", paths);
		Assert.Contains("build-tools;36.1.0", paths);
	}

	[Fact]
	public void List_InstalledPackages_HaveCorrectDetails()
	{
		var m = new SdkManager(tempSdkDir);
		var list = m.List();

		var pt = list.InstalledPackages.First(p => p.Path == "platform-tools");
		Assert.Equal("36.0.2", pt.Version);
		Assert.Equal("Android SDK Platform-Tools", pt.Description);
		Assert.NotEmpty(pt.Location);
	}

	[Fact]
	public void Start_ThrowsSdkManagerToolNotFoundException_WhenCmdlineToolsMissing()
	{
		var m = new SdkManager(tempSdkDir);
		m.SkipVersionCheck = true;

		var ex = Assert.Throws<SdkManagerToolNotFoundException>(() => m.GetVersion());
		Assert.NotNull(ex.SdkHome);
		Assert.Contains("cmdline-tools", ex.Message);
		Assert.Contains("android sdk download", ex.Message);
	}
}
