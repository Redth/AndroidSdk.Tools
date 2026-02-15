#nullable enable
using System.Linq;
using Xunit;

namespace AndroidSdk.Tests;

public class PackageManager_Tests
{
	[Fact]
	public void ParsePackageListOutputHandlesBase64LikePathsWithEquals()
	{
		var lines = new[]
		{
			"package:/data/app/~~wMmr-oLQ7RWpEYgnSyrqBQ==/com.companyname.TestApp-u2_pATr4m8RzW-4W-rLafw==/base.apk=com.companyname.TestApp installer=com.android.shell"
		};

		var packages = PackageManager.ParsePackageListOutput(lines);

		var package = Assert.Single(packages);
		Assert.Equal("com.companyname.TestApp", package.PackageName);
		Assert.Equal("com.android.shell", package.Installer);
		Assert.Contains("~~wMmr-oLQ7RWpEYgnSyrqBQ==", package.InstallPath.FullName);
		Assert.Contains("u2_pATr4m8RzW-4W-rLafw==", package.InstallPath.FullName);
	}

	[Fact]
	public void ParsePackageListOutputHandlesEmptyInstaller()
	{
		var lines = new[]
		{
			"package:/data/app/com.companyname.NoInstaller/base.apk=com.companyname.NoInstaller installer="
		};

		var package = Assert.Single(PackageManager.ParsePackageListOutput(lines));
		Assert.Equal("com.companyname.NoInstaller", package.PackageName);
		Assert.Equal(string.Empty, package.Installer);
	}

	[Fact]
	public void ParsePackageListOutputIgnoresNonMatchingLines()
	{
		var lines = new[]
		{
			"unexpected output",
			"package: missing bits"
		};

		Assert.Empty(PackageManager.ParsePackageListOutput(lines).ToList());
	}
}
