using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

[Collection(AndroidSdkManagerCollection.Name)]
public class EmulatorOperations_Tests : TestsBase
{
	const string StaticAppPackageName = "com.companyname.mauiapp12345";
	static readonly string StaticAppApkPath = Path.GetFullPath(Path.Combine(TestDataDirectory, "com.companyname.mauiapp12345-Signed.apk"));

	readonly AndroidSdkManager sdk;
	readonly DirectoryInfo androidSdkHome;
	readonly Emulator.AndroidEmulatorProcess emulatorInstance;

	public EmulatorOperations_Tests(ITestOutputHelper outputHelper, AndroidSdkManagerFixture fixture, BootedEmulatorFixture boot)
		: base(outputHelper)
	{
		boot.EnsureInitialized(fixture);
		sdk = fixture.Sdk;
		androidSdkHome = fixture.AndroidSdkHome;
		emulatorInstance = boot.EmulatorInstance;
		ResetSharedState();
	}

	void LogSdkException(string action, SdkToolFailedExitException ex)
	{
		OutputHelper.WriteLine($"{action} failed. ExitCode={ex.ExitCode} Message={ex.Message}");
		foreach (var line in ex.StdErr ?? Array.Empty<string>())
			OutputHelper.WriteLine($"stderr: {line}");
		foreach (var line in ex.StdOut ?? Array.Empty<string>())
			OutputHelper.WriteLine($"stdout: {line}");
	}

	void ResetSharedState()
	{
		try
		{
			var pm = new PackageManager(androidSdkHome, emulatorInstance.Serial);
			var isInstalled = pm.ListPackages().Any(p => p.PackageName == StaticAppPackageName);
			if (!isInstalled)
				return;

			sdk.Adb.Uninstall(StaticAppPackageName, adbSerial: emulatorInstance.Serial);
		}
		catch (SdkToolFailedExitException ex)
		{
			LogSdkException("ResetSharedState uninstall", ex);
		}
	}

	bool SupportsStaticAppAbi()
	{
		var abiList = sdk.Adb.Shell("getprop ro.product.cpu.abilist", emulatorInstance.Serial).FirstOrDefault() ?? string.Empty;
		var supportsX64 = abiList.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Any(abi => abi.Trim().Equals("x86_64", StringComparison.OrdinalIgnoreCase));

		if (!supportsX64)
			OutputHelper.WriteLine($"Emulator ABI list is '{abiList}' while test APK contains only x86_64.");

		return supportsX64;
	}

	string ResolveHomePackage()
	{
		var lines = sdk.Adb.Shell(
			"cmd package resolve-activity --brief -a android.intent.action.MAIN -c android.intent.category.HOME",
			emulatorInstance.Serial);

		var component = lines.LastOrDefault(line => line.Contains('/'));
		if (string.IsNullOrWhiteSpace(component))
			return "com.android.settings";

		return component.Split('/')[0];
	}

	public override void Dispose()
	{
		ResetSharedState();
		base.Dispose();
	}

	[Fact]
	public void BootedEmulatorHasExpectedIdentity()
	{
		Assert.Equal("emulator-5554", emulatorInstance.Serial);

		var devices = sdk.Adb.GetDevices();
		Assert.Contains(devices, d => d.Serial == emulatorInstance.Serial);

		var emuName = sdk.Adb.GetEmulatorName(emulatorInstance.Serial);
		Assert.Equal(emulatorInstance.AvdName, emuName);
	}

	[Fact]
	public void InstallVerifyAndUninstallStaticApp()
	{
		if (SupportsStaticAppAbi())
		{
			sdk.Adb.Install(new FileInfo(StaticAppApkPath), emulatorInstance.Serial);

			var pm = new PackageManager(androidSdkHome, emulatorInstance.Serial);
			var packagesAfterInstall = pm.ListPackages();
			Assert.Contains(packagesAfterInstall, p => p.PackageName == StaticAppPackageName);

			sdk.Adb.Uninstall(StaticAppPackageName, adbSerial: emulatorInstance.Serial);
			var packagesAfterUninstall = pm.ListPackages();
			Assert.DoesNotContain(packagesAfterUninstall, p => p.PackageName == StaticAppPackageName);
			return;
		}

		var ex = Assert.Throws<SdkToolFailedExitException>(
			() => sdk.Adb.Install(new FileInfo(StaticAppApkPath), emulatorInstance.Serial));
		var output = string.Join(
			Environment.NewLine,
			(ex.StdErr ?? Array.Empty<string>()).Concat(ex.StdOut ?? Array.Empty<string>()));
		Assert.Contains("NO_MATCHING_ABIS", output, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void LaunchStaticAppLaunches()
	{
		if (SupportsStaticAppAbi())
		{
			sdk.Adb.Install(new FileInfo(StaticAppApkPath), emulatorInstance.Serial);
			var staticAppOutput = sdk.Adb.LaunchApp(StaticAppPackageName, emulatorInstance.Serial);
			Assert.Contains(staticAppOutput, l => l.Contains("Events injected", StringComparison.OrdinalIgnoreCase));
			return;
		}

		var homePackage = ResolveHomePackage();
		var output = Array.Empty<string>();

		try
		{
			output = sdk.Adb.LaunchApp(homePackage, emulatorInstance.Serial).ToArray();
		}
		catch (SdkToolFailedExitException ex) when ((ex.StdOut?.Length > 0) || (ex.StdErr?.Length > 0))
		{
			LogSdkException($"Launch fallback package '{homePackage}'", ex);
			output = (ex.StdOut ?? Array.Empty<string>()).Concat(ex.StdErr ?? Array.Empty<string>()).ToArray();
		}

		Assert.Contains(output, line => line.Contains(":Monkey:", StringComparison.OrdinalIgnoreCase));
	}
}
