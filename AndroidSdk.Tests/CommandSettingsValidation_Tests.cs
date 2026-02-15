#nullable enable
using AndroidSdk.Tool;
using Xunit;

namespace AndroidSdk.Tests;

public class CommandSettingsValidation_Tests
{
	[Fact]
	public void DeviceLaunchCommandSettingsRequirePackage()
	{
		var settings = new DeviceLaunchCommandSettings { Package = "" };

		var result = settings.Validate();

		Assert.False(result.Successful);
		Assert.Contains("--package", result.Message);
	}

	[Fact]
	public void DeviceLaunchCommandSettingsAcceptPackage()
	{
		var settings = new DeviceLaunchCommandSettings { Package = "com.companyname.TestApp" };

		var result = settings.Validate();

		Assert.True(result.Successful);
	}

	[Fact]
	public void AvdDeleteCommandSettingsRequireName()
	{
		var settings = new AvdDeleteCommandSettings { Name = "" };

		var result = settings.Validate();

		Assert.False(result.Successful);
		Assert.Contains("--name", result.Message);
	}

	[Fact]
	public void AvdStartCommandSettingsRequireName()
	{
		var settings = new AvdStartCommandSettings { Name = "" };

		var result = settings.Validate();

		Assert.False(result.Successful);
		Assert.Contains("--name", result.Message);
	}

	[Fact]
	public void OutputFormatTypeConverterSupportsJsonPretty()
	{
		var converter = new OutputFormatTypeConverter();

		var format = converter.ConvertFrom("jsonpretty");

		Assert.Equal(OutputFormat.JsonPretty, format);
	}
}
