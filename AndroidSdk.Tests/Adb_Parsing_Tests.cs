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

}
