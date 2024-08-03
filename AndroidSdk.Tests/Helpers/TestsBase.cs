#nullable enable
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

/// <summary>
/// A base class for all tests in this assembly.
/// </summary>
public abstract class TestsBase
{
	public TestsBase(ITestOutputHelper outputHelper)
	{
		OutputHelper = outputHelper;
	}

	public ITestOutputHelper OutputHelper { get; private set; }

	public static string TestAssemblyDirectory => Utils.TestAssemblyDirectory;

	public static string TestDataDirectory => Utils.TestDataDirectory;
}
