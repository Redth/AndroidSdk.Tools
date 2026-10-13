#nullable enable
using AndroidSdk.Tool;
using Spectre.Console.Cli;
using Xunit;

namespace AndroidSdk.Tests;

public class CommandSettingsValidation_Tests
{
	[Theory]
	[InlineData("--target")]
	[InlineData("-t")]
	public void AvdCreateSettingsDoNotRecognizeTargetOption(string option)
	{
		var app = new CommandApp<NoOpAvdCreateCommand>();
		app.Configure(config =>
		{
			config.UseStrictParsing();
			config.PropagateExceptions();
		});

		var error = Assert.Throws<CommandParseException>(() => app.Run(new[]
		{
			"--name", "TestDevice",
			"--sdk", "system-images;android-37.1;google_apis_ps16k;arm64-v8a",
			option, "android-37.1"
		}));

		Assert.Contains("Unknown option", error.Message);
		Assert.Contains(option.TrimStart('-'), error.Message);
	}

	[Fact]
	public void AvdCreateCommandAcceptsSystemImageWithoutTargetOption()
	{
		var app = new CommandApp<NoOpAvdCreateCommand>();
		app.Configure(config =>
		{
			config.UseStrictParsing();
			config.PropagateExceptions();
		});

		Assert.Equal(0, app.Run(new[]
		{
			"--name", "TestDevice",
			"--sdk", "system-images;android-37.1;google_apis_ps16k;arm64-v8a"
		}));
	}

	public sealed class NoOpAvdCreateCommand : Command<AvdCreateCommandSettings>
	{
		public override int Execute(CommandContext context, AvdCreateCommandSettings settings) => 0;
	}

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
	public void AvdStartCommandSettingsAllowCores()
	{
		var settings = new AvdStartCommandSettings
		{
			Name = "Pixel",
			Cores = 2
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
