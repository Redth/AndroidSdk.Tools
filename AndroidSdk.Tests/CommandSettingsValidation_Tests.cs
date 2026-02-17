#nullable enable
using AndroidSdk.Tool;
using Xunit;

namespace AndroidSdk.Tests;

public class CommandSettingsValidation_Tests
{
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
	public void AvdDeleteCommandSettingsAllowForceWhenNameIsSet()
	{
		var settings = new AvdDeleteCommandSettings { Name = "Pixel", Force = true };

		var result = settings.Validate();

		Assert.True(result.Successful);
	}

	[Fact]
	public void AvdStartCommandSettingsAllowDisableAnimationsAndCpuThreshold()
	{
		var settings = new AvdStartCommandSettings
		{
			Name = "Pixel",
			DisableAnimations = true,
			CpuThreshold = 0.5
		};

		var result = settings.Validate();

		Assert.True(result.Successful);
	}

	[Fact]
	public void OutputFormatTypeConverterSupportsJsonPretty()
	{
		var converter = new OutputFormatTypeConverter();

		var format = converter.ConvertFrom("jsonpretty");

		Assert.Equal(OutputFormat.JsonPretty, format);
	}

	[Fact]
	public void OutputFormatTypeConverterSupportsJsonPrettyWithHyphen()
	{
		var converter = new OutputFormatTypeConverter();

		var format = converter.ConvertFrom("json-pretty");

		Assert.Equal(OutputFormat.JsonPretty, format);
	}
}
