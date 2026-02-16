using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// Booted emulator operation tests (identity, install/uninstall, and app launch).
/// </summary>
[Collection(nameof(AndroidSdkManagerCollection))]
public class EmulatorOperations_Tests :
    EmulatorTestsBase,
    IClassFixture<EmulatorTestsBase.AvdCreateFixture>,
    IClassFixture<EmulatorTestsBase.EmulatorBootFixture>
{
    const string StaticAppPackageName = "com.companyname.mauiapp12345";

    readonly EmulatorBootFixture boot;
    readonly Emulator.AndroidEmulatorProcess emulatorInstance;

    static readonly string StaticAppApkPath = Path.GetFullPath(Path.Combine(TestDataDirectory, "com.companyname.mauiapp12345-Signed.apk"));

    public EmulatorOperations_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture, EmulatorBootFixture boot)
        : base(outputHelper, fixture)
    {
        this.boot = boot;
        emulatorInstance = boot.EmulatorInstance;
    }

    public override void Dispose()
    {
        try
        {
            Sdk.Adb.Uninstall(StaticAppPackageName, adbSerial: boot.EmulatorInstance.Serial);
        }
        catch
        {
            // Ignore any exceptions from uninstall attempts in Dispose, as the app may not have been installed.
        }

        base.Dispose();
    }

    [Fact]
    public void BootedEmulatorHasExpectedIdentity()
    {
        Assert.Equal("emulator-5554", emulatorInstance.Serial);
        Assert.Equal(TestEmulatorName, emulatorInstance.AvdName);

        var devices = Sdk.Adb.GetDevices();
        Assert.Contains(devices, d => d.Serial == emulatorInstance.Serial);

        var emuName = Sdk.Adb.GetEmulatorName(emulatorInstance.Serial);
        Assert.Equal(TestEmulatorName, emuName);
    }

    [Fact]
    public void InstallVerifyAndUninstallStaticApp()
    {
        Sdk.Adb.Install(new FileInfo(StaticAppApkPath), emulatorInstance.Serial);

        var pm = new PackageManager(AndroidSdkHome, emulatorInstance.Serial);
        var packagesAfterInstall = pm.ListPackages();
        Assert.Contains(packagesAfterInstall, p => p.PackageName == StaticAppPackageName);

        Sdk.Adb.Uninstall(StaticAppPackageName, adbSerial: emulatorInstance.Serial);
        var packagesAfterUninstall = pm.ListPackages();
        Assert.DoesNotContain(packagesAfterUninstall, p => p.PackageName == StaticAppPackageName);
    }

    [Fact]
    public void LaunchStaticAppLaunches()
    {
        Sdk.Adb.Install(new FileInfo(StaticAppApkPath), emulatorInstance.Serial);

        var output = Sdk.Adb.LaunchApp(StaticAppPackageName, emulatorInstance.Serial);
        
        Assert.Contains(output, l => l.Contains("Events injected", StringComparison.OrdinalIgnoreCase));
    }
}
