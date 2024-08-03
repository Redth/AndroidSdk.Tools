#nullable enable
using System;
using System.IO;
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
	[InlineData("11.0", "11.0")]
	[InlineData("8.0", "8.0")]
	[InlineData(null, "8.0")]
	public async Task Download(string? version, string installVersion)
	{
		var path = Path.Combine(tempSdkPath, $"android-sdk-{version}");

		var sdk = new SdkManager();
		await sdk.DownloadSdk(destinationDirectory: new DirectoryInfo(path), specificVersion: version);

		Assert.True(
			File.Exists(Path.Combine(path, $"cmdline-tools/{installVersion}/lib/sdkmanager-classpath.jar")),
			"The sdkmanager-classpath.jar file did not exist in the new SDK location.");

		var isUpToDate = sdk.IsUpToDate();

		Assert.True(isUpToDate, "The new SDK was not up to date after updating.");
	}
}
