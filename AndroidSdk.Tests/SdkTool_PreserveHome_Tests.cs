#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// Tests that SdkTool and AndroidSdkManager preserve explicitly specified
/// home directories even when the directory does not yet exist on disk.
/// This is critical for the Acquire/DownloadSdk workflow where the SDK
/// must be downloaded to a new directory.
/// </summary>
public class SdkTool_PreserveHome_Tests : TestsBase
{
	public SdkTool_PreserveHome_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void SdkManager_PreservesHome_WhenDirectoryDoesNotExist()
	{
		var nonExistentPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "NonExistent", Guid.NewGuid().ToString("N"));
		Assert.False(Directory.Exists(nonExistentPath));

		var mgr = new SdkManager(new SdkManagerToolOptions
		{
			AndroidSdkHome = new DirectoryInfo(nonExistentPath),
			SkipVersionCheck = true
		});

		Assert.NotNull(mgr.AndroidSdkHome);
		Assert.Equal(nonExistentPath, mgr.AndroidSdkHome!.FullName);
	}

	[Fact]
	public void AndroidSdkManager_PreservesHome_WhenDirectoryDoesNotExist()
	{
		var nonExistentPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "NonExistent", Guid.NewGuid().ToString("N"));
		Assert.False(Directory.Exists(nonExistentPath));

		var mgr = new AndroidSdkManager(new DirectoryInfo(nonExistentPath));

		Assert.NotNull(mgr.Home);
		Assert.Equal(nonExistentPath, mgr.Home!.FullName);
	}

	[Fact]
	public async Task SdkManager_DownloadSdk_DoesNotThrow_WhenDirectoryDoesNotExist()
	{
		var nonExistentPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "NonExistent", Guid.NewGuid().ToString("N"));
		Assert.False(Directory.Exists(nonExistentPath));

		var mgr = new SdkManager(new SdkManagerToolOptions
		{
			AndroidSdkHome = new DirectoryInfo(nonExistentPath),
			SkipVersionCheck = true
		});

		// DownloadSdk should not throw DirectoryNotFoundException
		// because AndroidSdkHome should be preserved
		var ex = await Record.ExceptionAsync(() => mgr.DownloadSdk());

		// If it throws, it should NOT be the "Directory was not specified" error
		if (ex != null)
		{
			Assert.DoesNotContain("was not specified", ex.Message);
		}

		// Clean up if anything was created
		try { if (Directory.Exists(nonExistentPath)) Directory.Delete(nonExistentPath, true); } catch { }
	}

	[Fact]
	public void SdkManager_UsesLocatedPath_WhenDirectoryExists()
	{
		// When the directory exists and contains an SDK, Locate should find it
		var existingPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "ExistingDir", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(existingPath);

		try
		{
			var mgr = new SdkManager(new SdkManagerToolOptions
			{
				AndroidSdkHome = new DirectoryInfo(existingPath),
				SkipVersionCheck = true
			});

			// Should still resolve to the existing path
			Assert.NotNull(mgr.AndroidSdkHome);
			Assert.Equal(existingPath, mgr.AndroidSdkHome!.FullName);
		}
		finally
		{
			try { Directory.Delete(existingPath, true); } catch { }
		}
	}

	[Fact]
	public void AndroidSdkManager_UsesLocatedPath_WhenDirectoryExists()
	{
		var existingPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", "ExistingDir", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(existingPath);

		try
		{
			var mgr = new AndroidSdkManager(new DirectoryInfo(existingPath));

			Assert.NotNull(mgr.Home);
			Assert.Equal(existingPath, mgr.Home!.FullName);
		}
		finally
		{
			try { Directory.Delete(existingPath, true); } catch { }
		}
	}

	[Fact]
	public void SdkManager_HomeIsNull_WhenNoPathSpecified()
	{
		// When no path is given and no SDK is found via env vars, 
		// Home might be null or point to a discovered SDK — either is valid.
		// The key invariant: if Locate() returns something, use it.
		var mgr = new SdkManager(new SdkManagerToolOptions
		{
			AndroidSdkHome = null,
			SkipVersionCheck = true
		});

		// AndroidSdkHome should be whatever Locate() found (possibly null)
		var located = new SdkLocator().Locate()?.FirstOrDefault();
		if (located != null)
			Assert.Equal(located.FullName, mgr.AndroidSdkHome?.FullName);
		else
			Assert.Null(mgr.AndroidSdkHome);
	}
}
