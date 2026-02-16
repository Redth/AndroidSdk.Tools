using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

public class EmulatorOperations_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture, EmulatorOperations_Tests.OneTimeBoot boot)
    : EmulatorTestsBase(outputHelper, fixture), IClassFixture<EmulatorOperations_Tests.OneTimeBoot>
{
    const string StaticAppPackageName = "com.companyname.mauiapp12345";

    readonly Emulator.AndroidEmulatorProcess emulatorInstance = boot.EmulatorInstance;

    static readonly string StaticAppApkPath = Path.GetFullPath(Path.Combine(TestDataDirectory, "com.companyname.mauiapp12345-Signed.apk"));

    public class OneTimeBoot : OneTimeSetup
    {
		readonly IMessageSink sink;

        public OneTimeBoot(IMessageSink messageSink, AndroidSdkManagerFixture fixture)
            : base(messageSink, fixture)
        {
            sink = messageSink;

            sink.OnMessage(new DiagnosticMessage("Starting emulator for tests that require a booted emulator..."));
            EmulatorInstance = fixture.Sdk.Emulator.Start(TestEmulatorName, CreateHeadlessOptions(port: 5554));
            sink.OnMessage(new DiagnosticMessage("Started emulator."));

            sink.OnMessage(new DiagnosticMessage("Waiting for emulator to complete booting..."));
            var booted = EmulatorInstance.WaitForBootComplete(TimeSpan.FromMinutes(15));
            sink.OnMessage(new DiagnosticMessage("Emulator boot complete."));

            Assert.True(booted);
            Assert.NotEmpty(EmulatorInstance.Serial);
        }

        public Emulator.AndroidEmulatorProcess EmulatorInstance { get; }

        public override void Dispose()
        {
            sink.OnMessage(new DiagnosticMessage("Shutting down emulator..."));
            var shutdown = EmulatorInstance.Shutdown();
            Assert.True(shutdown);
            sink.OnMessage(new DiagnosticMessage("Shut down emulator."));

            base.Dispose();
        }
    }

    public override void Dispose()
    {
        try
        {
            outputHelper.WriteLine($"Uninstalling static app {StaticAppPackageName}...");
            Sdk.Adb.Uninstall(StaticAppPackageName, adbSerial: boot.EmulatorInstance.Serial);
            outputHelper.WriteLine("Uninstalled static app.");
        }
        catch (Exception ex)
        {
            outputHelper.WriteLine($"Failed to uninstall static app: {ex}");
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
