#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AndroidSdk.Tests;

public class SdkManager_CmdLineToolsVersionComparer_Tests : TestsBase, IDisposable
{
	private readonly DirectoryInfo tempDir;

	public SdkManager_CmdLineToolsVersionComparer_Tests(ITestOutputHelper outputHelper)
		: base(outputHelper)
	{
		var tempPath = Path.Combine(Path.GetTempPath(), "AndroidSdk.Tests", nameof(SdkManager_CmdLineToolsVersionComparer_Tests), Guid.NewGuid().ToString());
		RecreateDir(tempPath);

		tempDir = new DirectoryInfo(tempPath);
	}

	public void Dispose()
	{
		DeleteDir(tempDir.FullName);
	}

	[Theory]
	[InlineData(new[] { "1.0", "2.0" }, new[] { "1.0", "2.0" })]
	[InlineData(new[] { "2.0", "1.0" }, new[] { "1.0", "2.0" })]
	[InlineData(new[] { "2.0", "latest" }, new[] { "2.0", "latest" })]
	[InlineData(new[] { "1.0", "latest" }, new[] { "1.0", "latest" })]
	[InlineData(new[] { "latest", "1.0" }, new[] { "1.0", "latest" })]
	[InlineData(new[] { "latest", "2.0" }, new[] { "2.0", "latest" })]
	[InlineData(new[] { "20.0", "latest" }, new[] { "20.0", "latest" })]
	[InlineData(new[] { "11.0", "latest" }, new[] { "11.0", "latest" })]
	[InlineData(new[] { "latest", "11.0" }, new[] { "11.0", "latest" })]
	[InlineData(new[] { "latest", "20.0" }, new[] { "20.0", "latest" })]
	[InlineData(new[] { "11.0", "7.0" }, new[] { "7.0", "11.0" })]
	[InlineData(new[] { "11.0", "7.0", "latest" }, new[] { "7.0", "11.0", "latest" })]
	public void SortingWorksOnJustNames(string[] input, string[] expected)
	{
		CreateDirectories(input);

		var output = SdkManager.CmdLineToolsVersionComparer.Default.GetSortedDirectories(tempDir);

		Assert.Equal(expected, output.Select(d => d.Name));
	}

	[Theory]
	[InlineData(new[] { "11.0=11.0", "latest=9.0" }, new[] { "latest", "11.0" })]
	[InlineData(new[] { "11.0=11.0", "latest=13.0" }, new[] { "11.0", "latest" })]
	[InlineData(new[] { "11.0=11.0", "latest=9.0", "latest-2=13.0" }, new[] { "latest", "11.0", "latest-2" })]
	public void SortingWorksWithSourceProperties(string[] input, string[] expected)
	{
		CreateDirectories(input);

		var output = SdkManager.CmdLineToolsVersionComparer.Default.GetSortedDirectories(tempDir);

		Assert.Equal(expected, output.Select(d => d.Name));
	}

	[Theory]
	[InlineData(new[] { "11.0=11.0", "latest" }, new[] { "11.0", "latest" })]
	[InlineData(new[] { "9.0=9.0", "latest" }, new[] { "9.0", "latest" })]
	[InlineData(new[] { "11.0", "latest=9.0" }, new[] { "latest", "11.0" })]
	[InlineData(new[] { "9.0", "latest=11.0" }, new[] { "9.0", "latest" })]
	public void SortingWorksWithMixedSourceProperties(string[] input, string[] expected)
	{
		CreateDirectories(input);

		var output = SdkManager.CmdLineToolsVersionComparer.Default.GetSortedDirectories(tempDir);

		Assert.Equal(expected, output.Select(d => d.Name));
	}

	private void CreateDirectories(string[] inputDirs)
	{
		foreach (var dir in inputDirs)
		{
			var parts = dir.Split('=');
			var version = parts[0];

			var path = Path.Combine(tempDir.FullName, version);
			CreateDir(path);

			if (parts.Length == 2)
			{
				var revision = parts[1];
				File.WriteAllText(Path.Combine(path, "source.properties"), $"Pkg.Revision={revision}");
			}
		}
	}
}
