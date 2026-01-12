# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AndroidSdk.Tools is a .NET library and global dotnet tool for Android SDK management, providing programmatic access to ADB, AVD, Emulator, and SDK Manager functionality. The project consists of:

- **AndroidSdk** - Core library (netstandard2.0, net8.0) providing Android SDK management APIs
- **AndroidSdk.Tool** - Global dotnet tool (net8.0, net9.0, net10.0) exposing CLI commands via the `android` command
- **AndroidSdk.Adbd** - ADB daemon client library
- **AndroidRepository** - XML deserialization for Android SDK repository manifests (generated code)

## Development Commands

### Build
```bash
dotnet build
```

### Run Tests
```bash
# Run all tests
dotnet test --configuration Release

# Run tests with detailed logging (as used in CI)
dotnet test --configuration Release --logger:"console;verbosity=detailed" --logger:"trx;LogFileName=TestResults.trx"

# Run a specific test project
dotnet test AndroidSdk.Tests/AndroidSdk.Tests.csproj
dotnet test AndroidSdk.Adbd.Tests/AndroidSdk.Adbd.Tests.csproj
```

### Install and Run the Tool Locally
```bash
# Pack and install the tool from local build
dotnet pack AndroidSdk.Tool/AndroidSdk.Tool.csproj
dotnet tool install -g AndroidSdk.Tool --add-source ./AndroidSdk.Tool/bin/Debug

# Or run directly from the project
dotnet run --project AndroidSdk.Tool/AndroidSdk.Tool.csproj -- [commands]
```

### Tool Usage Examples
```bash
android sdk list --available
android sdk install --package emulator
android avd create --name MyEmulator --sdk "system-images;android-31;google_apis;x86_64" --device pixel
android avd start --name MyEmulator --wait-boot
android device list --format json
```

## Architecture

### Core Design Pattern

The library follows a consistent wrapper pattern around Android SDK command-line tools:

1. **SdkTool Base Class**: All tool wrappers (Adb, AvdManager, SdkManager, Emulator) inherit from `SdkTool`, which handles:
   - Locating the Android SDK home directory
   - Finding the tool executable path
   - Managing the `AndroidSdkHome` property

2. **ProcessRunner/JavaProcessRunner**: Low-level process execution with:
   - `ProcessArgumentBuilder` for safe argument construction
   - Standard output/error capture
   - Optional logging via `ANDROID_TOOL_PROCESS_RUNNER_LOG_PATH` environment variable
   - Exit code handling and `SdkToolFailedExitException` on non-zero exits

3. **Manager Classes**: Each wraps a specific Android SDK tool:
   - `Adb` (AndroidSdk/Adb/Adb.cs) - Wraps `adb` binary in platform-tools
   - `AvdManager` (AndroidSdk/AvdManager/AvdManager.cs) - Wraps `avdmanager` in cmdline-tools
   - `SdkManager` (AndroidSdk/SdkManager/SdkManager.cs) - Wraps `sdkmanager` in cmdline-tools, handles SDK acquisition
   - `Emulator` (AndroidSdk/Emulator/Emulator.cs) - Wraps `emulator` binary in emulator directory
   - `PackageManager` - Wraps `pm` shell commands via ADB
   - `ActivityManager` - Wraps `am` shell commands via ADB

4. **AndroidSdkManager**: The main entry point that aggregates all managers and provides a unified `Acquire()` method to download/install SDK tools if missing.

### Key Classes and Locations

- **AndroidSdkManager.cs** - Main facade providing unified access to all SDK tools
- **SdkTool.cs** - Base class for all tool wrappers
- **ProcessRunner.cs** - Non-Java process execution (adb, emulator)
- **JavaProcessRunner.cs** - Java process execution (sdkmanager, avdmanager)
- **ProcessArgumentBuilder.cs** - Safe command-line argument construction
- **SdkLocator.cs** - Finds Android SDK installations on the system
- **JdkLocator.cs** - Finds JDK installations (required for sdkmanager/avdmanager)

### Tool Path Resolution

Each tool implements `FindToolPath(DirectoryInfo androidSdkHome)` with specific logic:
- **SdkManager/AvdManager**: Check `cmdline-tools/*/bin/` first (newer), then `tools/bin/` (legacy)
- **Adb**: Look in `platform-tools/adb[.exe]`
- **Emulator**: Look in `emulator/emulator[.exe]`

### CLI Tool Architecture

The CLI tool (AndroidSdk.Tool) uses Spectre.Console.Cli for command routing:
- Commands organized in branches: `sdk`, `avd`, `device`, `apk`, `jdk`
- Each command class inherits from Spectre.Console.Cli command base classes
- Output formatting supports JSON, XML, or human-readable table formats via `--format` flag
- Uses `OutputHelper.cs` for consistent output rendering

### Test Architecture

Tests use xUnit with:
- **AndroidSdkManagerFixture**: Sets up a shared Android SDK instance for tests
- **AndroidSdkManagerCollection**: Groups tests that share the fixture
- **TestsBase** classes: Provide common setup/teardown logic
- Tests run on macOS in CI (requires emulator support)

## Important Implementation Details

### Exit Code Handling for Emulator

The emulator process can exit with exit code 1 when terminated normally (via SIGTERM). The code in `AvdStartCommand.cs` treats exit codes 0 and 1 as success to handle graceful shutdowns.

### Process Logging

Set these environment variables to debug underlying tool calls:
- `ANDROID_TOOL_PROCESS_RUNNER_LOG_PATH` - File path to write logs
- `ANDROID_TOOL_PROCESS_RUNNER_LOG_TYPES` - Pipe-delimited list of `stdout|stderr|stdin` (default: all)

### JDK Requirements

SdkManager and AvdManager require a JDK to run. The library uses `JdkLocator` to find installed JDKs and `JavaProcessRunner` to execute Java tools with the located JDK.

### SDK Acquisition

`SdkManager.Acquire()` downloads the Android SDK command-line tools if not present:
- Default version: 6858069
- Downloads from: `https://dl.google.com/android/repository/commandlinetools-{platform}-{version}_latest.zip`
- Extracts to `{AndroidSdkHome}/cmdline-tools/{version}/`

## Testing Notes

- Tests require a functional Android SDK installation (handled by fixture)
- Emulator tests require macOS or Linux with KVM support
- The test suite may take several minutes as it performs real SDK operations
- Test data is located in `AndroidSdk.Tests/testdata/`
