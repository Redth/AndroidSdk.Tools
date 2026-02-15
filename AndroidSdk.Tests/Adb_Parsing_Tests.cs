#nullable enable
using Xunit;

namespace AndroidSdk.Tests;

public class Adb_Parsing_Tests
{
	[Fact]
	public void IsLauncherInFocusReturnsTrueForLauncherFocusLine()
	{
		var lines = new[]
		{
			"Window #3 mCurrentFocus=Window{12345 u0 com.android.launcher3/com.android.launcher3.uioverrides.QuickstepLauncher}"
		};

		Assert.True(Adb.IsLauncherInFocus(lines));
	}

	[Fact]
	public void IsLauncherInFocusReturnsFalseWhenNoLauncherFocus()
	{
		var lines = new[]
		{
			"Window #3 mCurrentFocus=Window{12345 u0 com.companyname.testapp/com.companyname.testapp.MainActivity}"
		};

		Assert.False(Adb.IsLauncherInFocus(lines));
	}

	[Fact]
	public void TryParseLoadAverageParsesFirstValue()
	{
		var ok = Adb.TryParseLoadAverage("0.57 0.42 0.33 1/345 1024", out var load);

		Assert.True(ok);
		Assert.Equal(0.57, load, 2);
	}

	[Fact]
	public void TryParseLoadAverageReturnsFalseForInvalidLine()
	{
		var ok = Adb.TryParseLoadAverage("not-a-loadavg-line", out var load);

		Assert.False(ok);
		Assert.Equal(0, load);
	}

	[Fact]
	public void IsMonkeyLaunchSuccessfulDetectsInjectedEvents()
	{
		var lines = new[]
		{
			"Events injected: 1"
		};

		Assert.True(Adb.IsMonkeyLaunchSuccessful(lines));
	}
}
