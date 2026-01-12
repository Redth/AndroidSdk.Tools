# AndroidSdk.Mcp Server Specification

## Overview

This document specifies the design and implementation plan for an MCP (Model Context Protocol) server that exposes the AndroidSdk library functionality. The server will allow AI agents and applications to programmatically manage Android SDK installations, emulators, devices, and related tooling through a standardized protocol.

## Goals

1. **Expose core AndroidSdk functionality** through MCP tools, enabling AI-driven Android development workflows
2. **Maintain consistency** with the existing `AndroidSdk.Tool` CLI command structure
3. **Provide robust error handling** with meaningful error messages for AI agents
4. **Support both stdio and HTTP transports** for flexibility in deployment scenarios
5. **Leverage the official MCP C# SDK** (`ModelContextProtocol` NuGet package)

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      MCP Client                              │
│              (Claude, VS Code, etc.)                        │
└─────────────────────┬───────────────────────────────────────┘
                      │ MCP Protocol (stdio or HTTP)
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                   AndroidSdk.Mcp                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                 MCP Server Host                      │   │
│  │  (Microsoft.Extensions.Hosting + StdioTransport)    │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                  Tool Classes                        │   │
│  │  • SdkTools (list, install, download, info, etc.)   │   │
│  │  • AvdTools (list, create, delete, start)           │   │
│  │  • DeviceTools (list, info, install, uninstall)     │   │
│  │  • JdkTools (list, find)                            │   │
│  │  • ApkTools (manifest info)                         │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                  │
│                           ▼                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              AndroidSdkManager                       │   │
│  │        (Core library orchestration)                  │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    AndroidSdk Library                        │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐   │
│  │SdkManager│ │AvdManager│ │ Emulator │ │     Adb      │   │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘   │
└─────────────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              Android SDK Command Line Tools                  │
│        (sdkmanager, avdmanager, adb, emulator)              │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

```
AndroidSdk.Mcp/
├── AndroidSdk.Mcp.csproj
├── Program.cs                    # Entry point with MCP server setup
├── McpServerService.cs           # Hosted service wrapper (optional)
├── Tools/
│   ├── SdkTools.cs               # SDK management tools
│   ├── AvdTools.cs               # AVD/Emulator tools
│   ├── DeviceTools.cs            # ADB device tools
│   ├── JdkTools.cs               # JDK location tools
│   └── ApkTools.cs               # APK analysis tools
├── Resources/
│   └── SdkResources.cs           # Optional: expose SDK info as resources
└── README.md                     # Usage documentation
```

## MCP Tools Specification

### Tool Count Optimization

To minimize the number of tools exposed to AI agents (reducing context overhead), related operations are consolidated using action parameters where appropriate. **Total: 17 tools**

### 1. SDK Management Tools (`SdkTools`) - 5 tools

| Tool Name | Description | Parameters | Returns |
|-----------|-------------|------------|---------|
| `sdk_list` | List SDK packages (installed/available) | `installed?: bool`, `available?: bool`, `home?: string` | JSON array of packages |
| `sdk_package` | Install or uninstall SDK package(s) | `action: "install" \| "uninstall"`, `packages: string[]`, `home?: string` | Success/failure message |
| `sdk_download` | Download/bootstrap SDK cmdline-tools | `home: string`, `force?: bool` | Success/failure message |
| `sdk_info` | Get Android SDK environment info and path | `home?: string` | JSON with SDK path, version, installed tools, etc. |
| `sdk_licenses` | List or accept SDK licenses | `action: "list" \| "accept"`, `home?: string`, `force?: bool` | JSON array of licenses or success message |

### 2. AVD/Emulator Tools (`AvdTools`) - 3 tools

| Tool Name | Description | Parameters | Returns |
|-----------|-------------|------------|---------|
| `avd_list` | List AVDs, targets, or device definitions | `type: "avds" \| "targets" \| "devices"`, `home?: string` | JSON array of items |
| `avd_manage` | Create or delete an AVD | `action: "create" \| "delete"`, `name: string`, `sdk?: string`, `device?: string`, `force?: bool`, `home?: string` | Success/failure message |
| `avd_start` | Start an emulator | `name: string`, `waitBoot?: bool`, `noSnapshot?: bool`, `noWindow?: bool`, `home?: string` | Emulator serial or status |

### 3. Device/ADB Tools (`DeviceTools`) - 7 tools

| Tool Name | Description | Parameters | Returns |
|-----------|-------------|------------|---------|
| `device_list` | List connected devices/emulators | `home?: string` | JSON array of devices |
| `device_info` | Get device properties | `device?: string`, `properties?: string[]`, `home?: string` | JSON object with properties |
| `device_app` | Install or uninstall app on device | `action: "install" \| "uninstall"`, `package: string`, `device?: string`, `replace?: bool`, `keepData?: bool`, `home?: string` | Success/failure message |
| `device_shell` | Execute shell command on device | `command: string`, `device?: string`, `home?: string` | Command output |
| `device_screenshot` | Capture device screenshot | `outputPath: string`, `device?: string`, `home?: string` | Path to saved screenshot |
| `device_logcat` | Get device logs | `device?: string`, `maxLines?: int`, `home?: string` | Log lines |
| `adb_connection` | Connect or disconnect device over network | `action: "connect" \| "disconnect"`, `ip: string`, `port?: int`, `home?: string` | Success/failure message |

### 4. JDK Tools (`JdkTools`) - 1 tool

| Tool Name | Description | Parameters | Returns |
|-----------|-------------|------------|---------|
| `jdk_info` | List installed JDKs, optionally filter by version | `version?: int` | JSON array of JDK info (filtered if version specified) |

### 5. APK Tools (`ApkTools`) - 1 tool

| Tool Name | Description | Parameters | Returns |
|-----------|-------------|------------|---------|
| `apk_info` | Get APK manifest information | `apkPath: string` | JSON with package name, version, permissions, etc. |

## MCP Resources (Optional)

Resources provide read-only access to information:

| Resource URI | Description |
|--------------|-------------|
| `android://sdk/info` | Current SDK installation information |
| `android://sdk/packages/installed` | List of installed packages |
| `android://sdk/packages/available` | List of available packages |
| `android://devices` | Currently connected devices |
| `android://avds` | Available AVDs |

## Implementation Details

### Dependencies

```xml
<PackageReference Include="ModelContextProtocol" Version="0.1.0-*" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
<ProjectReference Include="..\AndroidSdk\AndroidSdk.csproj" />
```

### Target Framework

```xml
<TargetFramework>net10.0</TargetFramework>
```

### Tool Implementation Pattern

Each tool class will use the `[McpServerToolType]` attribute and individual methods will use `[McpServerTool]`:

```csharp
[McpServerToolType]
public class SdkTools
{
    private readonly AndroidSdkManager _sdkManager;
    
    public SdkTools(AndroidSdkManager sdkManager)
    {
        _sdkManager = sdkManager;
    }

    [McpServerTool(Name = "sdk_list")]
    [Description("Lists Android SDK packages. Returns installed and/or available packages.")]
    public async Task<SdkListResult> ListPackages(
        [Description("Include installed packages")] bool? installed = true,
        [Description("Include available packages")] bool? available = false,
        [Description("Android SDK home path (optional)")] string? home = null)
    {
        // Implementation
    }
}
```

### Error Handling Strategy

1. **Wrap AndroidSdk exceptions** in MCP-friendly error responses
2. **Validate inputs** before calling underlying SDK methods
3. **Provide actionable error messages** that help AI agents recover
4. **Log detailed errors** to stderr (MCP convention)

```csharp
try
{
    // SDK operation
}
catch (SdkToolFailedExitException ex)
{
    throw new McpProtocolException(
        $"SDK tool failed: {string.Join(", ", ex.StdErr)}",
        McpErrorCode.InternalError);
}
catch (FileNotFoundException ex)
{
    throw new McpProtocolException(
        $"Required tool not found: {ex.FileName}. Ensure Android SDK is installed.",
        McpErrorCode.InvalidRequest);
}
```

### Dependency Injection Setup

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<AndroidSdkManager>(sp => 
{
    var home = Environment.GetEnvironmentVariable("ANDROID_HOME");
    return new AndroidSdkManager(string.IsNullOrEmpty(home) ? null : new DirectoryInfo(home));
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ANDROID_HOME` | Default Android SDK path | Auto-detected |
| `JAVA_HOME` | Default JDK path | Auto-detected |
| `ANDROID_MCP_LOG_LEVEL` | Logging verbosity | `Information` |

### MCP Server Info

```json
{
  "name": "AndroidSdk.Mcp",
  "version": "1.0.0",
  "description": "MCP server for Android SDK management"
}
```

## Usage Examples

### VS Code MCP Configuration

```json
{
  "mcpServers": {
    "android-sdk": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/AndroidSdk.Mcp"],
      "env": {
        "ANDROID_HOME": "/Users/username/Library/Android/sdk"
      }
    }
  }
}
```

### Global Tool Installation (Future)

```bash
dotnet tool install -g AndroidSdk.Mcp
```

Then configure:

```json
{
  "mcpServers": {
    "android-sdk": {
      "command": "android-mcp"
    }
  }
}
```

## Testing Strategy

1. **Unit Tests**: Mock AndroidSdkManager and test tool methods in isolation
2. **Integration Tests**: Test against real SDK installation (CI environment)
3. **MCP Protocol Tests**: Verify JSON-RPC message format compliance

## Implementation Plan

### Phase 1: Foundation (Priority: High)
- [ ] Create `AndroidSdk.Mcp` project with basic structure (.NET 10)
- [ ] Implement MCP server host with stdio transport
- [ ] Implement `sdk_info` tool (validates setup, returns SDK path + environment info)
- [ ] Add to solution file
- [ ] Basic README documentation

### Phase 2: SDK Management (Priority: High)
- [ ] Implement `sdk_list` tool
- [ ] Implement `sdk_package` tool (install/uninstall)
- [ ] Implement `sdk_download` tool
- [ ] Implement `sdk_licenses` tool (list/accept)

### Phase 3: AVD Management (Priority: High)
- [ ] Implement `avd_list` tool (avds/targets/devices)
- [ ] Implement `avd_manage` tool (create/delete)
- [ ] Implement `avd_start` tool

### Phase 4: Device Operations (Priority: Medium)
- [ ] Implement `device_list` tool
- [ ] Implement `device_info` tool
- [ ] Implement `device_app` tool (install/uninstall)
- [ ] Implement `device_shell` tool
- [ ] Implement `device_screenshot` tool
- [ ] Implement `device_logcat` tool
- [ ] Implement `adb_connection` tool (connect/disconnect)

### Phase 5: Supporting Tools (Priority: Medium)
- [ ] Implement `jdk_info` tool
- [ ] Implement `apk_info` tool

### Phase 6: Resources & Polish (Priority: Low)
- [ ] Implement MCP resources (optional read-only data)
- [ ] Add HTTP transport support (ASP.NET Core)
- [ ] Create global tool packaging
- [ ] Comprehensive documentation
- [ ] CI/CD pipeline integration

## Security Considerations

1. **File System Access**: Tools that read/write files should validate paths
2. **Command Injection**: Shell commands must be properly escaped (handled by ProcessArgumentBuilder)
3. **Network Access**: ADB connect only allows specified IP/port combinations
4. **No Secrets**: Never log or return sensitive data (API keys, etc.)

## Success Criteria

1. All core CLI tool functionality exposed via MCP tools
2. AI agents (Claude, etc.) can successfully:
   - Query SDK installation status
   - Install/manage SDK packages
   - Create and manage emulators
   - Deploy apps to devices
   - Capture screenshots and logs
3. Error messages are actionable and help agents recover
4. Performance is acceptable (tools complete in reasonable time)
5. Documentation enables easy setup and usage

## Future Enhancements

1. **Progress Notifications**: Stream progress for long-running operations (SDK download, emulator boot)
2. **Prompts**: Pre-built prompts for common Android development tasks
3. **Watch Mode**: Real-time device/emulator status updates
4. **Build Integration**: Integration with Gradle/MSBuild for project building
