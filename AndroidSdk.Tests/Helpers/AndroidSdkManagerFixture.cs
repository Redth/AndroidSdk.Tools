#nullable enable
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

public class AndroidSdkManagerFixture : IAsyncLifetime
{
	private string? tempSdkPath;

	public const bool TryUsingGlobalSdk = true;

	public AndroidSdkManagerFixture(IMessageSink messageSink)
	{
		MessageSink = messageSink;
	}

	public IMessageSink MessageSink { get; }

	public DirectoryInfo AndroidSdkHome { get; private set; } = null!;

	public AndroidSdkManager Sdk { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		Sdk = await GetAndroidSdk();
	}

	public Task DisposeAsync()
	{
		if (!string.IsNullOrEmpty(tempSdkPath) && Directory.Exists(tempSdkPath))
			Directory.Delete(tempSdkPath, true);

		return Task.CompletedTask;
	}

	async Task<AndroidSdkManager> GetAndroidSdk()
	{
		if (TryUsingGlobalSdk)
		{
			var locator = new SdkLocator();
			var possible = locator.Locate();
			var globalSdk = possible.FirstOrDefault();

			if (globalSdk != null && globalSdk.Exists)
			{
				AndroidSdkHome = globalSdk;

				MessageSink.OnMessage(new DiagnosticMessage("Using GLOBAL android sdk: {0}", AndroidSdkHome));
			}
		}

		if (AndroidSdkHome == null || !AndroidSdkHome.Exists)
		{
			tempSdkPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AndroidSdkManagerFixture), "android-sdk");

			Directory.CreateDirectory(tempSdkPath);

			AndroidSdkHome = new DirectoryInfo(tempSdkPath);

			MessageSink.OnMessage(new DiagnosticMessage("Using TEMP android sdk: {0}", AndroidSdkHome));

			var s = new AndroidSdkManager(AndroidSdkHome);

			await s.Acquire();

			Assert.True(s.SdkManager.IsUpToDate());
		}

		return new AndroidSdkManager(AndroidSdkHome);
	}
}
