# AndroidSdk.Mcp.Server

An MCP (Model Context Protocol) server that exposes Android SDK management functionality to AI agents and applications.

## Overview

This MCP server provides tools for:

- **SDK Management**: List, install, and uninstall Android SDK packages
- **AVD Management**: Create, delete, and start Android emulators
- **Device Operations**: List devices, install apps, run shell commands, capture screenshots
- **JDK Discovery**: Find installed JDKs for Android development
- **APK Analysis**: Read APK manifest information

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- Android SDK (optional - can be downloaded via the `sdk_download` tool)

### Building

```bash
cd AndroidSdk.Mcp
dotnet build
```

## MCP Server Configuration

### Using `dnx`

```json
{
  "mcpServers": {
    "android-sdk": {
      "command": "dotnet",
      "args": ["dnx", "-y", "AndroidSdk.Mcp.Server"],
      "env": {
        "ANDROID_HOME": "/optional/path/to/android/sdk"
      }
    }
  }
}
```

### Using Installed tool / Older .NET

After installing with `dotnet tool install -g AndroidSdk.Mcp.Server`:

```json
{
  "mcpServers": {
    "android-sdk": {
      "command": "android-mcp",
      "env": {
        "ANDROID_HOME": "/optional/path/to/android/sdk"
      }
    }
  }
}
```

### Environment Variables

| Variable | Description |
|----------|-------------|
| `ANDROID_HOME` | Path to Android SDK. If not set, auto-detects from common locations. |
| `JAVA_HOME` | Path to JDK. If not set, auto-detects from common locations. |

## Available Tools

### SDK Management

| Tool | Description |
|------|-------------|
| `sdk_info` | Get Android SDK environment information and path |
| `sdk_list` | List installed and/or available SDK packages |
| `sdk_package` | Install or uninstall SDK packages |
| `sdk_download` | Download and bootstrap SDK cmdline-tools |
| `sdk_licenses` | List or accept SDK licenses |

### AVD/Emulator Management

| Tool | Description |
|------|-------------|
| `avd_list` | List AVDs, targets, or device definitions |
| `avd_manage` | Create or delete AVDs |
| `avd_start` | Start an emulator |

### Device Operations

| Tool | Description |
|------|-------------|
| `device_list` | List connected devices and emulators |
| `device_info` | Get device properties |
| `device_app` | Install or uninstall apps |
| `device_shell` | Execute shell commands on device |
| `device_screenshot` | Capture device screenshot |
| `device_logcat` | Get device log output |
| `adb_connection` | Connect/disconnect devices over network |

### JDK Management

| Tool | Description |
|------|-------------|
| `jdk_info` | List installed JDKs and find best for Android |

### APK Analysis

| Tool | Description |
|------|-------------|
| `apk_info` | Read APK manifest information |

## Examples

### Check SDK Installation

```
Tool: sdk_info
```

Returns SDK path, installed components, and version information.

### Install SDK Packages

```
Tool: sdk_package
Parameters:
  action: "install"
  packages: ["platform-tools", "platforms;android-34", "build-tools;34.0.0"]
```

### Create an Emulator

```
Tool: avd_manage
Parameters:
  action: "create"
  name: "Pixel_7_API_34"
  sdk: "system-images;android-34;google_apis;x86_64"
  device: "pixel_7"
```

### Start Emulator and Wait for Boot

```
Tool: avd_start
Parameters:
  name: "Pixel_7_API_34"
  waitBoot: true
```

### Install an APK

```
Tool: device_app
Parameters:
  action: "install"
  package: "/path/to/app.apk"
  replace: true
```

### Capture Screenshot

```
Tool: device_screenshot
Parameters:
  outputPath: "/path/to/screenshot.png"
```

## License

MIT License - see LICENSE file in repository root.
