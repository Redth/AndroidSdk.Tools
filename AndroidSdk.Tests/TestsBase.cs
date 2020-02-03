using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests
{
	public abstract class TestsBase
	{
		static DirectoryInfo AndroidSdkHome;

		public TestsBase(ITestOutputHelper outputHelper)
		{
			OutputHelper = outputHelper;
		}

		public ITestOutputHelper OutputHelper { get; private set; }

		public static string TestAssemblyDirectory
		{
			get
			{
				var codeBase = typeof(TestsBase).Assembly.CodeBase;
				var uri = new UriBuilder(codeBase);
				var path = Uri.UnescapeDataString(uri.Path);
				return Path.GetDirectoryName(path);
			}
		}

		public static string TestDataDirectory
		{
			get
			{
				if (System.Diagnostics.Debugger.IsAttached)
				{
					return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
						Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "testdata")
						: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "testdata");
				}

				return Path.Combine(TestAssemblyDirectory, "testdata");
			}
		}

		public AndroidSdkManager GetSdk(bool useGlobalSdk = false)
		{
			if (useGlobalSdk)
			{
				var globalSdk = AndroidSdkManager.FindHome()?.FirstOrDefault();

				if (globalSdk != null && globalSdk.Exists)
					AndroidSdkHome = globalSdk;
			}

			if (AndroidSdkHome == null || !AndroidSdkHome.Exists)
			{
				var sdkPath = Path.Combine(TestDataDirectory, "android-sdk");

				if (!Directory.Exists(sdkPath))
					Directory.CreateDirectory(sdkPath);

				AndroidSdkHome = new DirectoryInfo(sdkPath);

				var s = new AndroidSdkManager(AndroidSdkHome);

				s.Acquire();

				Assert.True(s.SdkManager.IsUpToDate());
			}

			return new AndroidSdkManager(AndroidSdkHome);
		}
	}
}
