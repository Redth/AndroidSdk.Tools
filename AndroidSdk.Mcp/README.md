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

Or get the image directly as base64:

```
Tool: device_screenshot
Parameters:
  returnBase64: true
```

## MCP Resources

The server also exposes read-only resources for querying information:

| Resource URI | Description |
|--------------|-------------|
| `android://sdk/info` | Current SDK installation information |
| `android://sdk/packages/installed` | List of installed packages |
| `android://sdk/packages/available` | List of available packages |
| `android://devices` | Currently connected devices |
| `android://avds` | Available AVDs |
| `android://avd/devices` | AVD device types (hardware profiles) |
| `android://devices/{device}/screenshot` | Capture screenshot from device |
| `android://devices/{device}/files/{+path}` | Pull file from device |

### Resource Examples

Read installed packages:
```
Resource: android://sdk/packages/installed
```

Get screenshot from a specific device:
```
Resource: android://devices/emulator-5554/screenshot
```

Pull a file from device:
```
Resource: android://devices/emulator-5554/files/sdcard/Download/data.json
```

## Progress Notifications

Long-running operations report progress to MCP clients:

- **`sdk_package`**: Reports progress during package installation/uninstallation
- **`sdk_download`**: Reports download progress (0-100%)
- **`avd_start`** (with `waitBoot: true`): Reports boot progress while waiting for emulator

## License

MIT License - see LICENSE file in repository root.
