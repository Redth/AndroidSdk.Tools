#nullable enable
using System.Linq;
using Xunit;

namespace AndroidSdk.Tests;

public class PackageManager_Tests
{
	[Theory]
	[InlineData("package:/data/app/~~wMmr-oLQ7RWpEYgnSyrqBQ==/com.companyname.TestApp-u2_pATr4m8RzW-4W-rLafw==/base.apk=com.companyname.TestApp installer=com.android.shell", "com.android.shell", "u2_pATr4m8RzW-4W-rLafw==")]
	[InlineData("package:/data/app/com.companyname.TestApp-plain/base.apk=com.companyname.TestApp installer=com.android.vending", "com.android.vending", "com.companyname.TestApp-plain")]
	public void ParsePackageListOutputHandlesInstallPathsWithAndWithoutEquals(string line, string installer, string pathFragment)
	{
		var lines = new[] { line };

		var packages = PackageManager.ParsePackageListOutput(lines);

		var package = Assert.Single(packages);
		Assert.Equal("com.companyname.TestApp", package.PackageName);
		Assert.Equal(installer, package.Installer);
		Assert.Contains(pathFragment, package.InstallPath.FullName);
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
