#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AndroidSdk.Tests;

public class AvdApiLevel_Tests : IDisposable
{
	private readonly DirectoryInfo root = Directory.CreateDirectory(
		Path.Combine(AppContext.BaseDirectory, "AvdApiLevelTests", Guid.NewGuid().ToString()));

	public void Dispose() => root.Delete(true);

	[Theory]
	[InlineData("android-31", "31")]
	[InlineData("platform-36", "36")]
	[InlineData("android-37.1", "37.1")]
	[InlineData("platform-37.10", "37.10")]
	[InlineData("android-37.0", "37.0")]
	[InlineData("android-0037.010", "37.10")]
	[InlineData("android-0", "0")]
	[InlineData("android-37.", null)]
	[InlineData("android-.1", null)]
	[InlineData("android-37.1.2", null)]
	[InlineData("android-37,1", null)]
	[InlineData("android--1", null)]
	[InlineData("android-+37", null)]
	[InlineData("android-37.-1", null)]
	[InlineData("android-2147483648", null)]
	[InlineData("android-37.2147483648", null)]
	[InlineData("android-٣٧.١", null)]
	[InlineData("android-preview", null)]
	[InlineData("android-0031", "31")]
	public void DetectsApiLevelFromTarget(string target, string? expected)
	{
		Assert.Equal(expected, CreateAvdInfo(target).ApiLevel);
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("fr-FR")]
	[InlineData("ar-SA")]
	public void DetectsMinorApiFromInstalledImageMetadata(string culture)
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
			var fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "testdata", "avd-api-level");
			var properties = File.ReadAllLines(Path.Combine(fixtureDirectory, "android-37.1.source.properties"))
				.Select(line => line.Split('=', 2))
				.ToDictionary(parts => parts[0], parts => parts[1]);
			var package = XDocument.Load(Path.Combine(fixtureDirectory, "android-37.1.package.xml"));
			var localPackage = package.Descendants().Single(element => element.Name.LocalName == "localPackage");
			var packageId = localPackage.Attribute("path")!.Value;
			var apiLevel = package.Descendants().Single(element => element.Name.LocalName == "api-level").Value;
			Assert.Equal(properties["AndroidVersion.ApiLevel"], apiLevel);

			var target = Assert.Single(AvdManager.ParseListTargetsOutput(TargetOutput(apiLevel)));
			Assert.Equal(37, target.ApiLevel);
			Assert.Equal(1, target.ApiLevelMinor);
			Assert.Equal("37.1", target.ApiLevelDisplay);
			Assert.Equal(target.ApiLevelDisplay, CreateAvdInfo(packageId.Split(';')[1]).ApiLevel);
			Assert.Equal(properties["SystemImage.Abi"], packageId.Split(';')[3]);
			Assert.Contains(" | 37.1 | ", target.ToString());
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}

	[Theory]
	[InlineData("31", 31, null, "31")]
	[InlineData("36", 36, null, "36")]
	[InlineData("37.1", 37, 1, "37.1")]
	[InlineData("37.10", 37, 10, "37.10")]
	[InlineData("37.0", 37, 0, "37.0")]
	[InlineData("0037.010", 37, 10, "37.10")]
	[InlineData(" 31 ", 31, null, "31")]
	public void ParsesIntegerAndMinorTargets(string api, int major, int? minor, string display)
	{
		var target = Assert.Single(AvdManager.ParseListTargetsOutput(TargetOutput(api)));
		Assert.Equal(major, target.ApiLevel);
		Assert.Equal(minor, target.ApiLevelMinor);
		Assert.Equal(display, target.ApiLevelDisplay);
		Assert.Equal("android-" + api, target.Id);
		Assert.Equal(1, target.NumericId);
		Assert.Equal(8, target.Revision);
	}

	[Theory]
	[InlineData("37.")]
	[InlineData(".1")]
	[InlineData("37.1.2")]
	[InlineData("37,1")]
	[InlineData("-1")]
	[InlineData("+37")]
	[InlineData("37.-1")]
	[InlineData("37.+1")]
	[InlineData("37. 1")]
	[InlineData("2147483648")]
	[InlineData("37.2147483648")]
	[InlineData("٣٧.١")]
	[InlineData("preview")]
	[InlineData("0")]
	public void SkipsInvalidTargetsWithoutDroppingValidNeighbors(string api)
	{
		var targets = AvdManager.ParseListTargetsOutput(TargetOutput(api).Concat(TargetOutput("31"))).ToArray();
		Assert.Equal(31, Assert.Single(targets).ApiLevel);
	}

	[Theory]
	[InlineData("31", null)]
	[InlineData("37.1", 1)]
	[InlineData("37.10", 10)]
	[InlineData("37.0", 0)]
	public void SerializesOptionalMinorWithoutChangingIntegerContract(string api, int? minor)
	{
		var target = Assert.Single(AvdManager.ParseListTargetsOutput(TargetOutput(api)));
		var json = JObject.Parse(JsonConvert.SerializeObject(target));
		Assert.Equal(JTokenType.Integer, json["apiLevel"]!.Type);
		Assert.Null(json["ApiLevelDisplay"]);
		if (minor.HasValue)
			Assert.Equal(minor.Value, (int)json["apiLevelMinor"]!);
		else
		{
			Assert.Null(json["apiLevelMinor"]);
			Assert.Equal(new[] { "apiLevel", "id", "name", "numericId", "revision", "type" },
				json.Properties().Select(property => property.Name).OrderBy(name => name));
		}
		var jsonRoundTrip = JsonConvert.DeserializeObject<AvdManager.AvdTarget>(json.ToString())!;
		Assert.Equal(target.ApiLevelDisplay, jsonRoundTrip.ApiLevelDisplay);

		var serializer = new XmlSerializer(typeof(AvdManager.AvdTarget));
		using var writer = new StringWriter(CultureInfo.InvariantCulture);
		serializer.Serialize(writer, target);
		var xml = XDocument.Parse(writer.ToString());
		Assert.Null(xml.Root!.Element("ApiLevelDisplay"));
		Assert.Equal(target.ApiLevel.ToString(CultureInfo.InvariantCulture), xml.Root.Element("ApiLevel")!.Value);
		Assert.Equal(minor?.ToString(CultureInfo.InvariantCulture), xml.Root.Element("ApiLevelMinor")?.Value);
		using var reader = new StringReader(writer.ToString());
		var xmlRoundTrip = (AvdManager.AvdTarget)serializer.Deserialize(reader)!;
		Assert.Equal(target.ApiLevelDisplay, xmlRoundTrip.ApiLevelDisplay);
	}

	[Theory]
	[InlineData(new[] { "latest=21.0", "19.0=19.0" }, "latest")]
	[InlineData(new[] { "19.0=19.0", "latest=21.0" }, "latest")]
	[InlineData(new[] { "21.0=21.0", "latest=19.0" }, "21.0")]
	[InlineData(new[] { "latest=19.0", "21.0=21.0" }, "21.0")]
	[InlineData(new[] { "21.0", "19.0" }, "21.0")]
	[InlineData(new[] { "latest", "19.0" }, "latest")]
	public void FindsNewestAvdManagerRegardlessOfDirectoryOrder(string[] directories, string expected)
	{
		foreach (var directory in directories)
		{
			var parts = directory.Split('=');
			CreateTool(Path.Combine("cmdline-tools", parts[0]), parts.Length == 2 ? parts[1] : null);
		}

		var tool = new AvdManager(root).FindToolPath(root);
		Assert.Equal(Path.Combine(root.FullName, "cmdline-tools", expected, "bin", ToolName), tool?.FullName);
	}

	[Fact]
	public void SkipsMissingExecutableAndFallsBackToLegacyTools()
	{
		var missing = Directory.CreateDirectory(Path.Combine(root.FullName, "cmdline-tools", "latest"));
		File.WriteAllText(Path.Combine(missing.FullName, "source.properties"), "Pkg.Revision=22.0");
		CreateTool("tools");
		var manager = new AvdManager(root);
		Assert.Equal(Path.Combine(root.FullName, "tools", "bin", ToolName), manager.FindToolPath(root)?.FullName);

		CreateTool(Path.Combine("cmdline-tools", "19.0"), "19.0");
		Assert.Equal(Path.Combine(root.FullName, "cmdline-tools", "19.0", "bin", ToolName), manager.FindToolPath(root)?.FullName);
	}

	private static string ToolName =>
		"avdmanager" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".bat" : "");

	private void CreateTool(string directory, string? revision = null)
	{
		var path = Path.Combine(root.FullName, directory);
		Directory.CreateDirectory(Path.Combine(path, "bin"));
		File.WriteAllText(Path.Combine(path, "bin", ToolName), "");
		if (revision is not null)
			File.WriteAllText(Path.Combine(path, "source.properties"), $"Pkg.Revision={revision}");
	}

	private AvdInfo CreateAvdInfo(string target)
	{
		var ini = new FileInfo(Path.Combine(root.FullName, "test.ini"));
		var config = new FileInfo(Path.Combine(root.FullName, "config.ini"));
		File.WriteAllText(ini.FullName, "target=" + target);
		File.WriteAllText(config.FullName, "");
		return new AvdInfo("test", root, ini, config);
	}

	private static string[] TargetOutput(string api) => new[]
	{
		$"id: 1 or \"android-{api}\"",
		"     Name: Android platform",
		"     Type: Platform",
		$"     API level: {api}",
		"     Revision: 8",
	};
}
