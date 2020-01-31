using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Android.Tools.Tests
{
	public abstract class TestsBase
	{
		static DirectoryInfo AndroidSdkHome;

		public TestsBase(ITestOutputHelper outputHelper)
		{
			OutputHelper = outputHelper;
		}

		public ITestOutputHelper OutputHelper { get; private set; }

		public DirectoryInfo EnsureAndroidSdkExists(bool useGlobalSdk = false)
		{
			if (useGlobalSdk)
			{
				var globalSdk = AndroidSdk.FindHome()?.FirstOrDefault();

				if (globalSdk != null && globalSdk.Exists)
					return globalSdk;
			}

			if (AndroidSdkHome == null || !AndroidSdkHome.Exists)
			{
				AndroidSdkHome = new DirectoryInfo(Path.GetTempPath());

				var d = new SdkManager(AndroidSdkHome);
				d.DownloadSdk(AndroidSdkHome);
				Assert.True(d.IsUpToDate());
			}

			return AndroidSdkHome;
		}

		public SdkManager GetSdkManager(bool useGlobalSdk = false)
		{
			var androidSdkHome = EnsureAndroidSdkExists(useGlobalSdk);

			return new SdkManager(androidSdkHome);
		}
	}
}
