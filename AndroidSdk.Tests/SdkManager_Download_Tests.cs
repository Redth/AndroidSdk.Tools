#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class SdkManager_Download_Tests : TestsBase
{
	string tempSdkPath;

	public SdkManager_Download_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
		tempSdkPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(SdkManager_Download_Tests));
	}

	public override void Dispose()
	{
		if (Directory.Exists(tempSdkPath))
			Directory.Delete(tempSdkPath, true);

		base.Dispose();
	}

	[Theory]
	//[InlineData("latest", "13.0")] // TODO: the number will change
	[InlineData("13.0", "13.0")]
	[InlineData("16.0", "16.0")]
	public async Task Download(string? version, string installVersion)
	{
		var path = Path.Combine(tempSdkPath, $"android-sdk-{version}");

		var sdk = new SdkManager();
		await sdk.DownloadSdk(destinationDirectory: new DirectoryInfo(path), specificVersion: version);

		if (!string.IsNullOrEmpty(version))
		{
			Assert.True(
				File.Exists(Path.Combine(path, $"cmdline-tools", installVersion, "lib", "sdkmanager-classpath.jar")),
				"The sdkmanager-classpath.jar file did not exist in the new SDK location.");
		}
		else
		{
			var installPath = Path.Combine(path, $"cmdline-tools");

			foreach (var p in Directory.GetDirectories(installPath))
			{
				Assert.True(
					File.Exists(Path.Combine(p, $"lib", "sdkmanager-classpath.jar")),
					"The sdkmanager-classpath.jar file did not exist in the new SDK location.");
			}
			
		}

			var isUpToDate = sdk.IsUpToDate();

		Assert.True(isUpToDate, "The new SDK was not up to date after updating.");
	}
}
