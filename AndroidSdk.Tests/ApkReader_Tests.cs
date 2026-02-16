using AndroidSdk.Apk;
using AndroidSdk.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class ApkReader_Tests(ITestOutputHelper outputHelper) : TestsBase(outputHelper)
{
	public readonly string ApkFile = Path.GetFullPath(Path.Combine(TestDataDirectory, "com.companyname.mauiapp12345-Signed.apk"));

	[Fact]
	public void ReadPackageId()
	{
		var reader = new ApkReader(ApkFile);

		var packageId = reader.ReadManifest().Manifest.PackageId;

		Assert.NotNull(packageId);
		Assert.Equal("com.companyname.mauiapp12345", packageId);
	}

	[Fact]
	public void ReadVersionName()
	{
		var reader = new ApkReader(ApkFile);

		var versionName = reader.ReadManifest().Manifest.VersionName;

		Assert.NotNull(versionName);
		Assert.Equal("1.0", versionName);
	}

	[Fact]
	public void ReadVersionCode()
	{
		var reader = new ApkReader(ApkFile);

		var versionCode = reader.ReadManifest().Manifest.VersionCode;

		Assert.NotEqual(0, versionCode);
		Assert.Equal(1, versionCode);
	}

	[Fact]
	public void ReadMinSdkVersion()
	{
		var reader = new ApkReader(ApkFile);

		var minSdkVersion = reader.ReadManifest().Manifest.UsesSdk.MinSdkVersion;

		Assert.Equal(21, minSdkVersion);
	}

	[Fact]
	public void ReadTargetSdkVersion()
	{
		var reader = new ApkReader(ApkFile);

		var targetSdkVersion = reader.ReadManifest().Manifest.UsesSdk.TargetSdkVersion;

		Assert.Equal(36, targetSdkVersion);
	}

	[Fact]
	public void ReadMaxSdkVersion()
	{
		var reader = new ApkReader(ApkFile);

		var maxSdkVersion = reader.ReadManifest().Manifest.UsesSdk.MaxSdkVersion;

		Assert.Equal(0, maxSdkVersion);
	}
}
