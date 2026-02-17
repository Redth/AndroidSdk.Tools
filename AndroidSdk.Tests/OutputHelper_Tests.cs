using System;
using System.IO;
using Xunit;
using AndroidSdk.Tool;

namespace AndroidSdk.Tests;

public class OutputHelper_Tests
{
	[Fact]
	public void OutputHelper_Json_Formats_Correctly()
	{
		var data = new { Name = "Test", Value = 123 };
		var oldOut = Console.Out;
		var sw = new StringWriter();
		
		try
		{
			Console.SetOut(sw);
			OutputHelper.Output(data, OutputFormat.Json);
			var output = sw.ToString();
			
			Assert.Contains("\"Name\":\"Test\"", output);
			Assert.Contains("\"Value\":123", output);
		}
		finally
		{
			Console.SetOut(oldOut);
		}
	}

	[Fact]
	public void OutputHelper_JsonPretty_Formats_Correctly()
	{
		var data = new { Name = "Test", Value = 123 };
		var oldOut = Console.Out;
		var sw = new StringWriter();
		
		try
		{
			Console.SetOut(sw);
			OutputHelper.Output(data, OutputFormat.JsonPretty);
			var output = sw.ToString();

			Assert.Contains("\"Name\": \"Test\"", output); // Note the space after colon
			Assert.Contains("\"Value\": 123", output);
			Assert.Contains(Environment.NewLine, output.Trim()); // Pretty JSON should have newlines
		}
		finally
		{
			Console.SetOut(oldOut);
		}
	}
}
