# APK Commands Reference

Commands for inspecting Android application packages (APK files).

## Table of Contents

- [apk manifest info](#apk-manifest-info)

---

## apk manifest info

Read and display APK manifest information.

```bash
android apk manifest info --package <apk-path> [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-p\|--pkg\|--package <path>` | **Required.** Path to APK file |
| `-f\|--format <json\|xml>` | Output format |

### Output Fields (Table Format)

- **Package ID**: Application package identifier
- **Version Code**: Numeric version (used for updates)
- **Version Name**: Human-readable version string
- **Minimum SDK**: Minimum Android API level required
- **Target SDK**: Target Android API level
- **Maximum SDK**: Maximum Android API level (if set)

### Output Formats

- **Default (table)**: Key-value table with essential manifest info
- **XML (`--format xml`)**: Full AndroidManifest.xml content
- **JSON (`--format json`)**: Full manifest as JSON

### Examples

```bash
# Get basic manifest info
android apk manifest info --package ./app-release.apk

# Get full manifest as XML
android apk manifest info --package ./app.apk --format xml

# Get manifest as JSON for parsing
android apk manifest info --package ./app.apk --format json
```

### Use Cases

#### Verify Build Configuration

```bash
# Check that release APK targets correct SDK
android apk manifest info --package ./app-release.apk
```

#### Script Integration

```bash
# Get package info as JSON for CI validation
info=$(android apk manifest info --package ./app.apk --format json)
package_id=$(echo "$info" | jq -r '.manifest.package')
version_code=$(echo "$info" | jq -r '.manifest."android:versionCode"')
```

#### Compare APK Versions

```bash
# Compare version codes between builds
echo "Debug:"
android apk manifest info --package ./app-debug.apk

echo "Release:"
android apk manifest info --package ./app-release.apk
```

---

## Common Manifest Properties

### Package Identifier

The unique application ID (e.g., `com.example.myapp`). This identifies the app on the device and in the Play Store.

### Version Code

Integer that Android uses to determine if an update is available. Must be incremented for each release.

### Version Name

Human-readable version string shown to users (e.g., `1.2.3`).

### SDK Versions

| Property | Description |
|----------|-------------|
| **minSdkVersion** | Minimum Android API level the app supports |
| **targetSdkVersion** | API level the app was designed and tested against |
| **maxSdkVersion** | Maximum API level (rarely used) |

### Common API Levels

| API Level | Android Version |
|-----------|-----------------|
| 34 | Android 14 |
| 33 | Android 13 |
| 32 | Android 12L |
| 31 | Android 12 |
| 30 | Android 11 |
| 29 | Android 10 |
| 28 | Android 9 (Pie) |
| 26 | Android 8.0 (Oreo) |
| 24 | Android 7.0 (Nougat) |
| 21 | Android 5.0 (Lollipop) |
