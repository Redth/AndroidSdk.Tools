using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using AndroidSdk.Tool;

namespace AndroidSdk.Tests;

public class OutputHelper_Tests
{
    public class TestData
    {
        public string Name { get; set; }
        public int Value { get; set; }
        
        public override string ToString() => $"{Name}:{Value}";
        
        // Needed for XmlSerializer deserialization if we were doing that, but for serialization strictly not needed 
        // unless we want to be safe. Public is required.
        public TestData() { }
    }

    [Fact]
    public void Output_Data_Json_Formats_Correctly()
    {
        var data = new TestData { Name = "Test", Value = 123 };
        // OutputHelper.Output(data, format) uses Console.Write, so no trailing newline
        var expected = "{\"Name\":\"Test\",\"Value\":123}";
        
        var actual = CaptureOutput(data, OutputFormat.Json);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Output_Data_JsonPretty_Formats_Correctly()
    {
        var data = new TestData { Name = "Test", Value = 123 };
        
        var actual = CaptureOutput(data, OutputFormat.JsonPretty);
        
        Assert.Contains("\"Name\": \"Test\"", actual);
        Assert.Contains("\"Value\": 123", actual);
        // Console.Write used, so maybe no newline at very end? 
        // But Json.NET indented adds newlines between properties.
        Assert.StartsWith("{", actual);
        Assert.EndsWith("}", actual); 
    }

    [Fact]
    public void Output_Data_Xml_Formats_Correctly()
    {
        var data = new TestData { Name = "Test", Value = 123 };
        
        var actual = CaptureOutput(data, OutputFormat.Xml);
        
        Assert.Contains("<?xml version=\"1.0\" encoding=\"utf-16\"?>", actual);
        Assert.Contains("<TestData", actual);
        Assert.Contains("<Name>Test</Name>", actual);
        Assert.Contains("<Value>123</Value>", actual);
        Assert.Contains("</TestData>", actual);
    }
    
    [Fact]
    public void Output_Items_None_Formats_Correctly()
    {
        var data = new[] { new TestData { Name = "A", Value = 1 }, new TestData { Name = "B", Value = 2 } };
        // Items with None format iterates and prints ToString() + NewLine
        var expected = "A:1" + Environment.NewLine + "B:2" + Environment.NewLine;
        
        var actual = CaptureOutput(data, OutputFormat.None);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Output_Items_Json_Formats_Correctly()
    {
        var data = new[] { new TestData { Name = "A", Value = 1 } };
        var expected = "[{\"Name\":\"A\",\"Value\":1}]" + Environment.NewLine;
        
        var actual = CaptureOutputItems(data, OutputFormat.Json);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Output_Items_JsonPretty_Formats_Correctly()
    {
        var data = new[] { new TestData { Name = "A", Value = 1 } };
        
        var actual = CaptureOutputItems(data, OutputFormat.JsonPretty);
        
        Assert.Contains("\"Name\": \"A\"", actual);
        Assert.Contains("\"Value\": 1", actual);
        Assert.StartsWith("[", actual.TrimStart());
        Assert.EndsWith("]", actual.TrimEnd());
    }

    [Fact]
    public void Output_Item_JsonPretty_Formats_Correctly()
    {
        var data = new TestData { Name = "A", Value = 1 };
        
        var actual = CaptureOutputItem(data, OutputFormat.JsonPretty);
        
        Assert.Contains("\"Name\": \"A\"", actual);
        Assert.Contains("\"Value\": 1", actual);
        Assert.StartsWith("{", actual.TrimStart());
        Assert.EndsWith("}", actual.TrimEnd());
    }

    [Fact]
    public void Output_Item_Json_Formats_Correctly()
    {
        var data = new TestData { Name = "A", Value = 1 };
        var expected = "{\"Name\":\"A\",\"Value\":1}" + Environment.NewLine;
        
        var actual = CaptureOutputItem(data, OutputFormat.Json);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Output_Enumerable_ToString_Formats_Correctly()
    {
        var data = new[] 
        { 
            new TestData { Name = "A", Value = 1 },
            new TestData { Name = "B", Value = 2 }
        };
        // Items with None format iterates and prints ToString() + NewLine
        var expected = "A:1" + Environment.NewLine + "B:2" + Environment.NewLine;
        
        var actual = CaptureOutput(data, OutputFormat.None);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Output_Items_Table_Formats_Correctly()
    {
        var data = new[] { new TestData { Name = "A", Value = 1 } };
        
        var actual = CaptureOutputItems(data, OutputFormat.None);
        
        Assert.Contains("Name", actual);
        Assert.Contains("Value", actual);
        Assert.Contains("A", actual);
        Assert.Contains("1", actual);
    }

    [Fact]
    public void Output_Item_Table_Formats_Correctly()
    {
        var data = new TestData { Name = "A", Value = 1 };
        
        var actual = CaptureOutputItem(data, OutputFormat.None);

        Assert.Contains("Property", actual);
        Assert.Contains("Value", actual);
        Assert.Contains("Name", actual);
        Assert.Contains("A", actual);
        Assert.Contains("1", actual);
    }

    private string CaptureOutput<T>(T data, OutputFormat format)
    {
        var sw = new StringWriter();
        
        // Capture Console.Out (for Json/Xml/None)
        var oldOut = Console.Out;
        Console.SetOut(sw);
        
        // Also capture AnsiConsole (for Table/None that might use it)
        var oldConsole = AnsiConsole.Console;
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings 
        { 
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(sw),
            Interactive = InteractionSupport.No,
            Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
        });

        try
        {
            OutputHelper.Output(data, format);
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(oldOut);
            AnsiConsole.Console = oldConsole;
        }
    }

    private string CaptureOutputItems<T>(IEnumerable<T> data, OutputFormat format)
    {
        var sw = new StringWriter();
        
        var oldOut = Console.Out;
        Console.SetOut(sw);
        
        var oldConsole = AnsiConsole.Console;
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings 
        { 
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(sw),
            Interactive = InteractionSupport.No,
            Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
        });

        try
        {
            // Explicitly call the IEnumerable overload
            OutputHelper.Output<T>(data, format, new[] { "Name", "Value" }, i => 
            {
                var p = i as TestData;
                return p != null ? new[] { p.Name, p.Value.ToString() } : new[] { i.ToString(), "" };
            });
            sw.Flush();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(oldOut);
            AnsiConsole.Console = oldConsole;
        }
    }
    
    private string CaptureOutputItem<T>(T data, OutputFormat format)
    {
        var sw = new StringWriter();
        
        var oldOut = Console.Out;
        Console.SetOut(sw);
        
        var oldConsole = AnsiConsole.Console;
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings 
        { 
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(sw),
            Interactive = InteractionSupport.No,
            Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
        });

        try
        {
            OutputHelper.Output(data, format, new[] { "Name", "Value" }, i => 
            {
                var p = i as TestData;
                return p != null ? new[] { p.Name, p.Value.ToString() } : new[] { i.ToString(), "" };
            });
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(oldOut);
            AnsiConsole.Console = oldConsole;
        }
    }
}
