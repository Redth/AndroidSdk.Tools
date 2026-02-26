#nullable enable
using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class SdkComponentScanner_Tests : TestsBase
{
	public SdkComponentScanner_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void Scan_NullSdkHome_ReturnsEmptyInventory()
	{
		var scanner = new SdkComponentScanner();
		var inventory = scanner.Scan(null);

		Assert.NotNull(inventory);
		Assert.Empty(inventory.Components);
		Assert.False(inventory.HasCmdlineTools);
		Assert.False(inventory.HasPlatformTools);
		Assert.False(inventory.HasEmulator);
	}

	[Fact]
	public void Scan_NonExistentDirectory_ReturnsEmptyInventory()
	{
		var scanner = new SdkComponentScanner();
		var inventory = scanner.Scan(new DirectoryInfo("/nonexistent/path/that/does/not/exist"));

		Assert.NotNull(inventory);
		Assert.Empty(inventory.Components);
	}

	[Fact]
	public void Scan_EmptyDirectory_ReturnsEmptyInventory()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "SdkComponentScanner", Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			var scanner = new SdkComponentScanner();
			var inventory = scanner.Scan(new DirectoryInfo(tempDir));

			Assert.NotNull(inventory);
			Assert.Empty(inventory.Components);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void Scan_WithPackageXml_ParsesComponent()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "SdkComponentScanner", Guid.NewGuid().ToString());
		var platformToolsDir = Path.Combine(tempDir, "platform-tools");
		Directory.CreateDirectory(platformToolsDir);

		try
		{
			// Write a minimal package.xml
			File.WriteAllText(Path.Combine(platformToolsDir, "package.xml"), @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ns2:repository xmlns:ns2=""http://schemas.android.com/repository/android/common/02"">
    <localPackage path=""platform-tools"" obsolete=""false"">
        <revision><major>36</major><minor>0</minor><micro>2</micro></revision>
        <display-name>Android SDK Platform-Tools</display-name>
    </localPackage>
</ns2:repository>");

			var scanner = new SdkComponentScanner();
			var inventory = scanner.Scan(new DirectoryInfo(tempDir));

			Assert.Single(inventory.Components);
			var component = inventory.Components[0];
			Assert.Equal("platform-tools", component.Path);
			Assert.Equal("Android SDK Platform-Tools", component.DisplayName);
			Assert.Equal(new Version(36, 0, 2), component.Version);
			Assert.True(inventory.HasPlatformTools);
			Assert.False(inventory.HasCmdlineTools);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void Scan_MultipleComponents_ParsesAll()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "SdkComponentScanner", Guid.NewGuid().ToString());

		var dirs = new[]
		{
			("platform-tools", "platform-tools", "Android SDK Platform-Tools", "36", "0", "2"),
			("emulator", "emulator", "Android Emulator", "36", "3", "10"),
			("build-tools/36.1.0", "build-tools;36.1.0", "Android SDK Build-Tools 36.1", "36", "1", "0"),
		};

		try
		{
			foreach (var (dir, path, name, major, minor, micro) in dirs)
			{
				var fullDir = Path.Combine(tempDir, dir);
				Directory.CreateDirectory(fullDir);
				File.WriteAllText(Path.Combine(fullDir, "package.xml"), $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ns2:repository xmlns:ns2=""http://schemas.android.com/repository/android/common/02"">
    <localPackage path=""{path}"">
        <revision><major>{major}</major><minor>{minor}</minor><micro>{micro}</micro></revision>
        <display-name>{name}</display-name>
    </localPackage>
</ns2:repository>");
			}

			var scanner = new SdkComponentScanner();
			var inventory = scanner.Scan(new DirectoryInfo(tempDir));

			Assert.Equal(3, inventory.Components.Count);
			Assert.True(inventory.HasPlatformTools);
			Assert.True(inventory.HasEmulator);
			Assert.True(inventory.HasBuildTools);
			Assert.False(inventory.HasCmdlineTools);
			Assert.False(inventory.HasPlatforms);
			Assert.False(inventory.HasSystemImages);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void Scan_WithCmdlineTools_DetectsPresence()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "SdkComponentScanner", Guid.NewGuid().ToString());
		var cmdlineDir = Path.Combine(tempDir, "cmdline-tools", "19.0");
		Directory.CreateDirectory(cmdlineDir);

		try
		{
			File.WriteAllText(Path.Combine(cmdlineDir, "package.xml"), @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ns2:repository xmlns:ns2=""http://schemas.android.com/repository/android/common/02"">
    <localPackage path=""cmdline-tools;19.0"">
        <revision><major>19</major><minor>0</minor></revision>
        <display-name>Android SDK Command-line Tools</display-name>
    </localPackage>
</ns2:repository>");

			var scanner = new SdkComponentScanner();
			var inventory = scanner.Scan(new DirectoryInfo(tempDir));

			Assert.Single(inventory.Components);
			Assert.True(inventory.HasCmdlineTools);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[Fact]
	public void Scan_InvalidPackageXml_SkipsGracefully()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "SdkComponentScanner", Guid.NewGuid().ToString());
		var invalidDir = Path.Combine(tempDir, "bad-component");
		var validDir = Path.Combine(tempDir, "emulator");
		Directory.CreateDirectory(invalidDir);
		Directory.CreateDirectory(validDir);

		try
		{
			// Write an invalid package.xml
			File.WriteAllText(Path.Combine(invalidDir, "package.xml"), "this is not valid xml");

			// Write a valid one
			File.WriteAllText(Path.Combine(validDir, "package.xml"), @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<ns2:repository xmlns:ns2=""http://schemas.android.com/repository/android/common/02"">
    <localPackage path=""emulator"">
        <revision><major>36</major></revision>
        <display-name>Android Emulator</display-name>
    </localPackage>
</ns2:repository>");

			var scanner = new SdkComponentScanner();
			var inventory = scanner.Scan(new DirectoryInfo(tempDir));

			// Should have parsed the valid one and skipped the invalid one
			Assert.Single(inventory.Components);
			Assert.Equal("emulator", inventory.Components[0].Path);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}
}
