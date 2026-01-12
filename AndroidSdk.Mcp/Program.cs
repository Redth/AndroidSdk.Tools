using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using AndroidSdk;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr (MCP convention - stdout is for protocol messages)
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register AndroidSdkManager as a singleton
builder.Services.AddSingleton(sp =>
{
    var home = Environment.GetEnvironmentVariable("ANDROID_HOME");
    return new AndroidSdkManager(string.IsNullOrEmpty(home) ? null : new DirectoryInfo(home));
});

// Configure MCP server with stdio transport and tools from this assembly
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "AndroidSdk.Mcp",
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
