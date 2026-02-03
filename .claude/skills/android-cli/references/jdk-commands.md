# JDK Commands Reference

Commands for discovering Java Development Kits and configuring .NET Android builds.

## Table of Contents

- [jdk list](#jdk-list)
- [jdk find](#jdk-find)
- [jdk dotnet-prefer](#jdk-dotnet-prefer)

---

## jdk list

List discovered JDK installations on the system.

```bash
android jdk list [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-v\|--version <range>` | Filter by version (NuGet version range syntax) |
| `-h\|--home <path>` | Specific JDK home to include |
| `-p\|--path <path>` | Additional paths to search (repeatable) |
| `-f\|--format <json\|xml>` | Output format |

### Output Fields

- **Version**: JDK version
- **Path**: JDK home directory
- **Java**: Path to `java` executable
- **JavaC**: Path to `javac` executable
- **DotNet Preferred**: Whether .NET is configured to use this JDK
- **From Env Var**: Whether discovered via `JAVA_HOME` environment variable

### Examples

```bash
# List all JDKs
android jdk list

# List JDKs version 17 or higher
android jdk list --version ">=17.0.0"

# List JDKs in specific version range
android jdk list --version "[17.0.0, 22.0.0)"

# Include custom path
android jdk list --path /opt/custom-jdk

# Get JSON output
android jdk list --format json
```

### Version Range Syntax

Uses NuGet version range syntax:

| Syntax | Description |
|--------|-------------|
| `17.0.0` | Minimum version 17.0.0 |
| `[17.0.0]` | Exact version 17.0.0 |
| `[17.0.0, 21.0.0]` | Range inclusive |
| `[17.0.0, 21.0.0)` | Range, exclusive upper bound |
| `>=17.0.0` | Version 17.0.0 or higher |

### Search Locations

The tool searches:
- `JAVA_HOME` environment variable
- Platform-specific standard locations:
  - **macOS**: `/Library/Java/JavaVirtualMachines/*/Contents/Home`
  - **Linux**: `/usr/lib/jvm/*`
  - **Windows**: `C:\Program Files\Java\*`, `C:\Program Files\Microsoft\jdk-*`
- .NET Android workload configuration

---

## jdk find

Find and output the best matching JDK home path.

```bash
android jdk find [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-v\|--version <range>` | Version filter (default: `>=17.0.0`) |

Returns the `JAVA_HOME` path for the newest JDK matching the version criteria.

### Examples

```bash
# Find newest JDK (17+)
android jdk find

# Find JDK 17 specifically
android jdk find --version "17.*"

# Find JDK 21 or higher
android jdk find --version ">=21.0.0"

# Use in script
export JAVA_HOME=$(android jdk find)
```

### Notes

- By default, requires JDK 17+ (required for modern Android tooling)
- Returns the newest version when multiple JDKs match
- Returns nothing if no matching JDK is found

---

## jdk dotnet-prefer

Set the preferred JDK for .NET Android/MAUI builds.

```bash
android jdk dotnet-prefer --home <path>
```

### Options

| Option | Description |
|--------|-------------|
| `-h\|--home <path>` | **Required.** JDK home path to prefer |

This updates the .NET workload configuration so that Android builds use the specified JDK.

### Examples

```bash
# Set preferred JDK
android jdk dotnet-prefer --home /Library/Java/JavaVirtualMachines/jdk-17.jdk/Contents/Home

# Use with auto-discovered JDK
android jdk dotnet-prefer --home $(android jdk find)

# Set specific version
android jdk dotnet-prefer --home $(android jdk find --version "17.*")
```

### Where Configuration is Stored

The .NET Android workload stores preferences in:
- **macOS/Linux**: `~/.config/Microsoft/Android/sdk.config`
- **Windows**: `%LOCALAPPDATA%\Microsoft\Android\sdk.config`

---

## JDK Requirements

### Android SDK Tools

- **sdkmanager**: Requires JDK to run (uses Java)
- **avdmanager**: Requires JDK to run (uses Java)
- **adb/emulator**: Do not require JDK (native binaries)

### .NET Android Builds

- **.NET 8+**: Requires JDK 17+
- **.NET 7**: Requires JDK 11+
- Build process uses JDK for:
  - D8/R8 dexing
  - AAPT2 resource processing
  - APK signing

### Recommended JDKs

| JDK | Notes |
|-----|-------|
| Microsoft Build of OpenJDK | Recommended for .NET development |
| Eclipse Temurin (Adoptium) | Popular open-source option |
| Amazon Corretto | AWS-supported OpenJDK |
| Oracle JDK | Requires license for commercial use |

---

## Common Workflows

### Setup for .NET MAUI Development

```bash
# Find and set preferred JDK
android jdk dotnet-prefer --home $(android jdk find)

# Verify
android jdk list
```

### CI/CD JDK Configuration

```bash
# Verify JDK is available
if jdk_path=$(android jdk find --version ">=17.0.0"); then
    echo "Found JDK at: $jdk_path"
    export JAVA_HOME="$jdk_path"
else
    echo "No suitable JDK found"
    exit 1
fi
```

### Multiple JDK Management

```bash
# List all JDKs
android jdk list

# Use JDK 17 for Android builds
android jdk dotnet-prefer --home $(android jdk find --version "[17.0.0, 18.0.0)")

# Verify configuration
android sdk info
```
