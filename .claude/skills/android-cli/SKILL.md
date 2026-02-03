---
name: android-cli
description: "Reference for the AndroidSdk.Tool CLI (`android` command) - a .NET global tool for Android SDK automation. Use when: (1) Managing Android SDK packages (list, install, uninstall, download), (2) Working with AVDs/emulators (create, delete, start, list), (3) Interacting with connected devices via ADB (list devices, get properties, install/uninstall APKs), (4) Locating or configuring JDK for Android development, (5) Setting up CI/CD for Android or .NET MAUI development, (6) Reading APK manifest information, (7) Accepting SDK licenses non-interactively."
---

# AndroidSdk.Tool CLI Reference

The `android` CLI is a .NET global tool providing programmatic access to Android SDK management. Install with:

```bash
dotnet tool install -g AndroidSdk.Tool
```

## Command Groups

| Group | Purpose |
|-------|---------|
| `sdk` | SDK package management (list, install, download, licenses) |
| `avd` | Android Virtual Device management (create, delete, start) |
| `device` | Connected device operations via ADB |
| `jdk` | JDK discovery and configuration |
| `apk` | APK inspection tools |

## Quick Start Examples

```bash
# Find SDK and show info
android sdk find
android sdk info

# List available and installed packages
android sdk list --available
android sdk list --installed

# Install emulator and system image
android sdk install --package emulator
android sdk install --package "system-images;android-34;google_apis;x86_64"

# Create and start an emulator
android avd create --name TestDevice --sdk "system-images;android-34;google_apis;x86_64" --device pixel_6
android avd start --name TestDevice --wait-boot

# List connected devices and install APK
android device list
android device install --package ./app.apk
```

## Common Options

Most commands support:
- `-f|--format <json|xml>` - Output as JSON or XML instead of table
- `-h|--home <path>` - Specify Android SDK home path (overrides auto-detection)

## Detailed Command Reference

For complete parameter details, see:

- **SDK Management**: [references/sdk-commands.md](references/sdk-commands.md) - Package listing, installation, downloads, licenses
- **AVD/Emulator**: [references/avd-emulator.md](references/avd-emulator.md) - Creating, configuring, and launching emulators
- **Device/ADB**: [references/device-adb.md](references/device-adb.md) - Device discovery, properties, app installation
- **JDK Management**: [references/jdk-commands.md](references/jdk-commands.md) - JDK location and .NET configuration
- **APK Inspection**: [references/apk-commands.md](references/apk-commands.md) - Reading APK manifest information

## CI/CD Workflow Example

```bash
# Download SDK to specific location (for CI)
android sdk download --home /opt/android-sdk --force

# Accept all licenses non-interactively
android sdk accept-licenses --force

# Install required components
android sdk install --package "platform-tools"
android sdk install --package "emulator"
android sdk install --package "platforms;android-34"
android sdk install --package "system-images;android-34;google_apis;x86_64"

# Create headless emulator
android avd create --name CI_Emulator --sdk "system-images;android-34;google_apis;x86_64" --device pixel_6 --force

# Start emulator in headless mode and wait for boot
android avd start --name CI_Emulator --no-window --no-audio --no-boot-anim --wait-boot --timeout 300

# Run tests, then devices are cleaned up when CI job ends
```

## Setting .NET MAUI/Xamarin Preferred Paths

Configure the SDK/JDK paths that .NET Android workloads will use:

```bash
# Set preferred Android SDK for .NET builds
android sdk dotnet-prefer --home /path/to/android-sdk

# Set preferred JDK for .NET builds
android jdk dotnet-prefer --home /path/to/jdk
```
