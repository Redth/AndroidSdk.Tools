#nullable enable
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

public class AndroidSdkManagerFixture : IAsyncLifetime
{
	private string? tempSdkPath;
	private static readonly object systemImageLock = new();
	private static bool systemImagePrepared;

	public const bool TryUsingGlobalSdk = true;
	public static readonly string TestAvdPackageId =
		RuntimeInformation.ProcessArchitecture == Architecture.Arm64
			? "system-images;android-30;google_apis;arm64-v8a"
			: "system-images;android-30;google_apis;x86_64";

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
		EnsureTestSystemImage();
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

	void EnsureTestSystemImage()
	{
		lock (systemImageLock)
		{
			if (systemImagePrepared)
				return;

			var list = Sdk.SdkManager.List();
			if (list.InstalledPackages.Any(p => p.Path == TestAvdPackageId))
			{
				MessageSink.OnMessage(new DiagnosticMessage("System image already installed: {0}", TestAvdPackageId));
				systemImagePrepared = true;
				return;
			}

			MessageSink.OnMessage(new DiagnosticMessage("Installing system image for tests: {0}", TestAvdPackageId));
			var installOk = Sdk.SdkManager.Install(TestAvdPackageId);
			Assert.True(installOk);

			list = Sdk.SdkManager.List();
			Assert.Contains(TestAvdPackageId, list.InstalledPackages.Select(p => p.Path));
			systemImagePrepared = true;
		}
	}
}
