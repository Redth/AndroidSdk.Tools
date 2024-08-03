using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class AvdLocator_Tests : TestsBase
{
	public AvdLocator_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
	}

	[Fact]
	public void LocatedPath()
	{
		var l = new AvdLocator();
		var p = l.Locate();

		Assert.NotNull(p);
		Assert.NotEmpty(p);
	}

	[Fact]
	public void LocatedPathForEnvVar()
	{
		var oldHome = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");

		var tempAvdPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(AvdLocator_Tests), nameof(LocatedPathForEnvVar), "android-avd-home");
		Directory.CreateDirectory(tempAvdPath);

		try
		{
			Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", tempAvdPath);

			var l = new AvdLocator();
			var p = l.Locate();

			Assert.NotNull(p);
			Assert.NotEmpty(p);

			Assert.Equal(tempAvdPath, p[0].FullName);
		}
		finally
		{
			Environment.SetEnvironmentVariable("ANDROID_AVD_HOME", oldHome);
			Directory.Delete(tempAvdPath, true);
		}
	}
}
