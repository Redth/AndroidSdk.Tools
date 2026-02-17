#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AndroidSdk.Tests;

/// <summary>
/// Shared SDK fixture that can use a global SDK when enabled and otherwise
/// provisions an isolated temp SDK once per collection and
/// only cleans up SDK directories that it created and owns.
/// </summary>
public class AndroidSdkManagerFixture(IMessageSink messageSink) : IAsyncLifetime
{
	private const bool TryUsingGlobalSdk = true;

	private string? tempSdkPath;

	public IMessageSink MessageSink { get; } = messageSink;

	public DirectoryInfo AndroidSdkHome { get; private set; } = null!;

	public AndroidSdkManager Sdk { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		Sdk = await GetAndroidSdk();
	}

	public Task DisposeAsync()
	{
		if (string.IsNullOrEmpty(tempSdkPath) || !Directory.Exists(tempSdkPath))
			return Task.CompletedTask;

		try
		{
			Directory.Delete(tempSdkPath, true);
		}
		catch (IOException ex)
		{
			MessageSink.OnMessage(new DiagnosticMessage("Failed to delete TEMP android sdk at {0}: {1}", tempSdkPath, ex.Message));
		}
		catch (UnauthorizedAccessException ex)
		{
			MessageSink.OnMessage(new DiagnosticMessage("Failed to delete TEMP android sdk at {0}: {1}", tempSdkPath, ex.Message));
		}

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
			tempSdkPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AndroidSdkManagerFixture), Guid.NewGuid().ToString("N"), "android-sdk");

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
