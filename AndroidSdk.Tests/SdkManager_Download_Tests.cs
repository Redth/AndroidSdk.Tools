#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class SdkManager_Download_Tests : TestsBase, IDisposable
{
	string tempSdkPath;

	public SdkManager_Download_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
		tempSdkPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(SdkManager_Download_Tests));
	}

	public void Dispose()
	{
		if (Directory.Exists(tempSdkPath))
			Directory.Delete(tempSdkPath, true);
	}

	[Theory]
	//[InlineData("latest", "13.0")] // TODO: the number will change
	[InlineData("13.0", "13.0")]
	[InlineData("16.0", "16.0")]
	public async Task Download(string? version, string installVersion)
	{
		var path = new DirectoryInfo(Path.Combine(tempSdkPath, $"android-sdk-{version}"));
		var jdk = new JdkLocator().Locate().First();

		var sdkDownloader = new SdkDownloader(jdk);
		var v = Version.Parse(installVersion);

		await sdkDownloader.DownloadAsync(destinationDirectory: path, specificVersion: (v.Major, v.Minor));

		if (!string.IsNullOrEmpty(version))
		{
			Assert.True(
				File.Exists(Path.Combine(path.FullName, $"cmdline-tools", installVersion, "lib", "sdkmanager-classpath.jar")),
				"The sdkmanager-classpath.jar file did not exist in the new SDK location.");
		}
		else
		{
			var installPath = Path.Combine(path.FullName, $"cmdline-tools");

			foreach (var p in Directory.GetDirectories(installPath))
			{
				Assert.True(
					File.Exists(Path.Combine(p, $"lib", "sdkmanager-classpath.jar")),
					"The sdkmanager-classpath.jar file did not exist in the new SDK location.");
			}
			
		}

		
		var sdk = new SdkManager(path, jdk);
		var isUpToDate = sdk.IsUpToDate();

		Assert.True(isUpToDate, "The new SDK was not up to date after updating.");
	}
}
