# Device & ADB Commands Reference

Commands for interacting with connected Android devices and emulators via ADB.

## Table of Contents

- [device list](#device-list)
- [device info](#device-info)
- [device install](#device-install)
- [device uninstall](#device-uninstall)

---

## device list

List connected Android devices and running emulators.

```bash
android device list [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Output Fields

- **Serial**: Device serial number or emulator identifier
- **Emulator**: Whether it's an emulator (True/False)
- **Device**: Device codename
- **Model**: Device model name
- **Product**: Product name

### Examples

```bash
# List all devices
android device list

# Get JSON for scripting
android device list --format json
```

### Serial Number Patterns

- Physical devices: Alphanumeric string (e.g., `RF8M33XXXXX`)
- USB-connected: May show as IP:port if wireless debugging enabled
- Emulators: `emulator-5554`, `emulator-5556`, etc.
- Network ADB: `192.168.1.100:5555`

---

## device info

Get device properties (similar to `adb shell getprop`).

```bash
android device info [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-d\|--id\|--device\|--serial <pattern>` | Filter by serial (supports regex) |
| `-p\|--prop\|--property <name>` | Property name filter (supports regex) |
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# Get all properties from all devices
android device info

# Get specific property
android device info --property ro.product.model

# Get properties matching pattern
android device info --property "ro\.product\..*"

# Filter by device serial
android device info --device emulator-5554

# Filter device by regex
android device info --device "emulator.*"

# Combined filters
android device info --device "emulator.*" --property ro.build.version.sdk
```

### Common Properties

| Property | Description |
|----------|-------------|
| `ro.product.model` | Device model |
| `ro.product.brand` | Device brand |
| `ro.product.manufacturer` | Manufacturer |
| `ro.build.version.sdk` | API level |
| `ro.build.version.release` | Android version |
| `ro.product.cpu.abi` | CPU architecture |
| `ro.build.type` | Build type (user, userdebug, eng) |
| `ro.hardware` | Hardware name |
| `ro.serialno` | Serial number |

### Device Selection Patterns

The `--device` option supports both exact matches and regex:

```bash
# Exact match
--device emulator-5554

# Regex patterns
--device "emulator.*"       # Any emulator
--device "192\.168\..*"     # Network devices
--device "RF8.*"            # Samsung devices starting with RF8
```

---

## device install

Install an APK to a connected device.

```bash
android device install --package <apk-path> [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-p\|--pkg\|--package <path>` | **Required.** Path to APK file |
| `-d\|--id\|--device\|--serial <pattern>` | Target device (required if multiple) |
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# Install to only connected device
android device install --package ./app-debug.apk

# Install to specific device
android device install --device emulator-5554 --package ./app.apk

# Install to emulator using pattern
android device install --device "emulator.*" --package ./app.apk
```

### Notes

- If multiple devices are connected, you must specify `--device`
- The APK file must exist at the specified path
- Re-installing updates the existing app (preserves data)

---

## device uninstall

Uninstall an app from a connected device.

```bash
android device uninstall --package <package-name> [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-p\|--pkg\|--package <name>` | **Required.** Package name (e.g., `com.example.app`) |
| `-k\|--keep-data` | Keep app data and cache directories |
| `-d\|--id\|--device\|--serial <pattern>` | Target device (required if multiple) |
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# Uninstall app
android device uninstall --package com.example.myapp

# Uninstall but keep data
android device uninstall --package com.example.myapp --keep-data

# Uninstall from specific device
android device uninstall --device emulator-5554 --package com.example.myapp
```

### Notes

- Use the package name (e.g., `com.example.app`), not the APK filename
- `--keep-data` preserves app data for potential reinstallation
- System apps cannot be uninstalled without root

---

## Common Workflows

### Install and Test App

```bash
# List devices
android device list

# Install APK
android device install --package ./app-debug.apk --device emulator-5554

# Verify installation by checking properties
android device info --device emulator-5554 --property ro.build.version.sdk
```

### Multi-Device Deployment

```bash
# Get device list as JSON for scripting
devices=$(android device list --format json)

# Install to first emulator found
android device install --device "emulator.*" --package ./app.apk
```

### Device Discovery for CI

```bash
# Check if any device is connected
if android device list --format json | grep -q "Serial"; then
    echo "Device connected"
    android device install --package ./app.apk
else
    echo "No device found"
    exit 1
fi
```
