#nullable enable
using System;
using System.IO;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// A base class for all tests in this assembly.
/// </summary>
public abstract class TestsBase
{
	protected const string? SkipOnCI =
#if IS_ON_CI
		"This test cannot run on CI.";
#else
		null;
#endif

	public TestsBase(ITestOutputHelper outputHelper)
	{
		OutputHelper = outputHelper;
	}

	public ITestOutputHelper OutputHelper { get; private set; }

	public static string TestAssemblyDirectory => Utils.TestAssemblyDirectory;

	public static string TestDataDirectory => Utils.TestDataDirectory;

	protected void Retry(Func<bool> test, Action action)
	{
		var tries = 10;
		while (test() && tries > 0)
		{
			try
			{
				action();
			}
			catch
			{
			}

			if (test())
			{
				Thread.Sleep(500);
				tries--;
			}
		}

		if (test())
			Assert.Fail("Timed out trying to perform the action.");
	}

	protected void DeleteDir(string dir) =>
		Retry(() => Directory.Exists(dir), () => Directory.Delete(dir, true));

	protected void CreateDir(string dir) =>
		Retry(() => !Directory.Exists(dir), () => Directory.CreateDirectory(dir));

	protected void RecreateDir(string dir)
	{
		DeleteDir(dir);
		CreateDir(dir);
	}
}
