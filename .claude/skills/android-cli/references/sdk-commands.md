# SDK Commands Reference

Commands for managing Android SDK packages, licenses, and configuration.

## Table of Contents

- [sdk list](#sdk-list)
- [sdk install](#sdk-install)
- [sdk uninstall](#sdk-uninstall)
- [sdk download](#sdk-download)
- [sdk info](#sdk-info)
- [sdk find](#sdk-find)
- [sdk licenses](#sdk-licenses)
- [sdk accept-licenses](#sdk-accept-licenses)
- [sdk dotnet-prefer](#sdk-dotnet-prefer)

---

## sdk list

List Android SDK packages (available and/or installed).

```bash
android sdk list [options]
```

### Options

| Option | Description |
|--------|-------------|
| `--available` | Show only packages available for installation |
| `--installed` | Show only installed packages |
| `--all` | Show all packages |
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# List all packages (available + installed)
android sdk list

# List only available packages
android sdk list --available

# List installed packages as JSON
android sdk list --installed --format json
```

### Output Fields

- **Package**: Package path/identifier (e.g., `platforms;android-34`)
- **Version**: Package version
- **Description**: Human-readable description
- **Location**: Installation path (installed packages only)

---

## sdk install

Install or update Android SDK packages.

```bash
android sdk install --package <package> [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-p\|--package <package>` | Package(s) to install (repeatable) |
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# Install single package
android sdk install --package emulator

# Install multiple packages
android sdk install --package "platforms;android-34" --package "platform-tools"

# Install system image
android sdk install --package "system-images;android-34;google_apis;x86_64"

# Install build tools
android sdk install --package "build-tools;34.0.0"
```

### Common Packages

| Package | Description |
|---------|-------------|
| `platform-tools` | ADB and fastboot |
| `emulator` | Android Emulator |
| `platforms;android-XX` | Android platform (replace XX with API level) |
| `build-tools;XX.X.X` | Build tools version |
| `system-images;android-XX;google_apis;ARCH` | System image for emulator |
| `cmdline-tools;latest` | Command-line tools |
| `ndk;XX.X.XXXXX` | Native Development Kit |

---

## sdk uninstall

Uninstall Android SDK packages.

```bash
android sdk uninstall --package <package> [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-p\|--package <package>` | Package(s) to uninstall (repeatable) |
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# Uninstall emulator
android sdk uninstall --package emulator

# Uninstall old platform
android sdk uninstall --package "platforms;android-30"
```

---

## sdk download

Download a fresh copy of the Android SDK command-line tools. Useful for bootstrapping a new SDK installation.

```bash
android sdk download --home <path> [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-h\|--home <path>` | **Required.** Target directory for SDK |
| `-f\|--force` | Delete existing directory contents first |
| `--preview` | Allow preview/beta versions |
| `--version <version>` | Specific version to download |
| `--arch <arch>` | Architecture: `x64`, `aarch64` |
| `--os <os>` | OS: `windows`, `linux`, `macos` |

### Examples

```bash
# Download SDK to new directory
android sdk download --home ~/android-sdk

# Force reinstall
android sdk download --home ~/android-sdk --force

# Download specific architecture for CI cross-platform
android sdk download --home /opt/android-sdk --os linux --arch x64
```

---

## sdk info

Display Android SDK installation information.

```bash
android sdk info [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Output Fields

- **Path**: SDK installation path
- **Version**: SDK tools version
- **IsUpToDate**: Whether tools are up to date
- **Channel**: Update channel (stable, beta, etc.)
- **DotNetPreferred**: Whether this SDK is the .NET preferred location
- **WriteAccess**: Whether the SDK directory is writable

Also lists discovered JDKs with their versions and paths.

### Examples

```bash
# Show SDK info
android sdk info

# Get JSON output for scripting
android sdk info --format json
```

---

## sdk find

Find and output the Android SDK home path.

```bash
android sdk find
```

Searches standard locations and outputs the discovered `ANDROID_HOME` path. Useful for scripts that need to determine SDK location.

### Examples

```bash
# Get SDK path
android sdk find

# Use in script
export ANDROID_HOME=$(android sdk find)
```

---

## sdk licenses

List Android SDK licenses and their acceptance status.

```bash
android sdk licenses [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-a\|--accepted` | Show only accepted licenses |
| `-u\|--unaccepted` | Show only unaccepted licenses |
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# List all licenses
android sdk licenses

# Check for unaccepted licenses
android sdk licenses --unaccepted

# Get license status as JSON
android sdk licenses --format json
```

---

## sdk accept-licenses

Accept Android SDK licenses (required before installing some packages).

```bash
android sdk accept-licenses [options]
```

### Options

| Option | Description |
|--------|-------------|
| `--force` | Accept all licenses without prompting |
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# Interactive acceptance
android sdk accept-licenses

# Non-interactive (for CI/automation)
android sdk accept-licenses --force
```

---

## sdk dotnet-prefer

Set the preferred Android SDK location for .NET Android/MAUI builds.

```bash
android sdk dotnet-prefer --home <path>
```

### Options

| Option | Description |
|--------|-------------|
| `-h\|--home <path>` | **Required.** Android SDK path to prefer |

This updates the .NET workload configuration so that `dotnet build` for Android projects uses this SDK.

### Examples

```bash
# Set preferred SDK
android sdk dotnet-prefer --home /opt/android-sdk

# Use with auto-discovered path
android sdk dotnet-prefer --home $(android sdk find)
```
