# AVD & Emulator Commands Reference

Commands for managing Android Virtual Devices (AVDs) and running the emulator.

## Table of Contents

- [avd list](#avd-list)
- [avd targets](#avd-targets)
- [avd devices](#avd-devices)
- [avd create](#avd-create)
- [avd delete](#avd-delete)
- [avd start](#avd-start)

---

## avd list

List existing Android Virtual Devices.

```bash
android avd list [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Output Fields

- **Name**: AVD name (used with `avd start`)
- **Target**: Target Android version
- **Device**: Hardware device profile
- **Based On**: System image used
- **Path**: AVD directory location

### Examples

```bash
# List all AVDs
android avd list

# Get JSON for scripting
android avd list --format json
```

---

## avd targets

List available targets (Android API levels) for creating AVDs.

```bash
android avd targets [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Output Fields

- **Name**: Target name
- **Id**: Target identifier
- **Numeric Id**: Numeric identifier
- **API Level**: Android API level
- **Type**: Platform type
- **Revision**: Target revision

### Examples

```bash
# List available targets
android avd targets
```

---

## avd devices

List available hardware device profiles for creating AVDs.

```bash
android avd devices [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-f\|--format <json\|xml>` | Output format |
| `-h\|--home <path>` | Android SDK home path |

### Output Fields

- **Name**: Device profile name
- **Id**: Device identifier (use with `--device`)
- **NumericId**: Numeric identifier
- **Oem**: Device manufacturer (Google, etc.)

### Examples

```bash
# List device profiles
android avd devices

# Common device IDs include:
# pixel, pixel_2, pixel_3, pixel_4, pixel_5, pixel_6, pixel_7
# Nexus 5, Nexus 6, Nexus 7
# And TV, Wear, Automotive profiles
```

---

## avd create

Create a new Android Virtual Device.

```bash
android avd create --name <name> --sdk <sdk-id> [options]
```

### Required Options

| Option | Description |
|--------|-------------|
| `-n\|--name <name>` | **Required.** AVD name |
| `-s\|--sdk\|--sdkid <id>` | **Required.** System image package ID |

### Optional Options

| Option | Description |
|--------|-------------|
| `-d\|--device <id>` | Hardware device profile |
| `-t\|--target <id>` | Target ID |
| `-p\|--path <path>` | Custom AVD directory |
| `-a\|--abi <abi>` | ABI (auto-selected if only one) |
| `--skin <name>` | Skin name |
| `--sdcard-path <path>` | SD card image path |
| `--sdcard-size <MB>` | SD card size in MB |
| `-f\|--force` | Overwrite existing AVD |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# Create basic emulator
android avd create \
  --name MyEmulator \
  --sdk "system-images;android-34;google_apis;x86_64" \
  --device pixel_6

# Create with specific options
android avd create \
  --name TestDevice \
  --sdk "system-images;android-34;google_apis_playstore;x86_64" \
  --device pixel_7 \
  --sdcard-size 512 \
  --force

# Create for ARM architecture (Apple Silicon)
android avd create \
  --name ArmEmulator \
  --sdk "system-images;android-34;google_apis;arm64-v8a" \
  --device pixel_6
```

### Common System Images

First install the system image with `android sdk install`:

| System Image | Description |
|--------------|-------------|
| `system-images;android-34;google_apis;x86_64` | Android 14, Google APIs, Intel/AMD |
| `system-images;android-34;google_apis;arm64-v8a` | Android 14, Google APIs, ARM |
| `system-images;android-34;google_apis_playstore;x86_64` | Android 14, with Play Store |
| `system-images;android-33;google_apis;x86_64` | Android 13 |
| `system-images;android-31;google_apis;x86_64` | Android 12 |

---

## avd delete

Delete an existing AVD.

```bash
android avd delete --name <name> [options]
```

### Options

| Option | Description |
|--------|-------------|
| `-n\|--name <name>` | **Required.** AVD name to delete |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# Delete AVD
android avd delete --name MyEmulator
```

---

## avd start

Start an AVD emulator.

```bash
android avd start --name <name> [options]
```

### Required Options

| Option | Description |
|--------|-------------|
| `-n\|--name <name>` | **Required.** AVD name to start |

### Startup Options

| Option | Description |
|--------|-------------|
| `-w\|--wait\|--wait-boot` | Wait for emulator to finish booting |
| `--wait-exit` | Wait for emulator process to exit |
| `-t\|--timeout <seconds>` | Boot timeout in seconds |
| `--wipe\|--wipe-data` | Wipe user data before starting |

### Snapshot Options

| Option | Description |
|--------|-------------|
| `--no-snapshot` | Disable snapshot load/save |
| `--no-snapshot-load` | Disable snapshot load only |
| `--no-snapshot-save` | Disable snapshot save only |

### Display Options

| Option | Description |
|--------|-------------|
| `--no-window` | Run headless (no graphical window) |
| `--no-boot-anim\|--no-boot-animation` | Disable boot animation (faster boot) |
| `--gpu <mode>` | GPU emulation mode |

### Hardware Options

| Option | Description |
|--------|-------------|
| `--memory <MB>` | RAM size (1536-8192 MB) |
| `--partition-size\|--data-partition-size <MB>` | Data partition size |
| `--cache-size\|--cache-partition-size <MB>` | Cache partition size (default: 66 MB) |
| `-p\|--port <port>` | Console/ADB port (5554-5682) |

### Emulation Options

| Option | Description |
|--------|-------------|
| `--engine <engine>` | Emulator engine: `auto`, `classic`, `qemu2` |
| `--accel\|--acceleration <mode>` | Acceleration: `auto`, `off`, `on` |
| `--screen <mode>` | Touch screen: `touch`, `multi-touch`, `no-touch` |
| `--camera-back <mode>` | Back camera: `emulated`, `webcam0`, `none` |
| `--camera-front <mode>` | Front camera: `emulated`, `webcam0`, `none` |
| `--no-audio` | Disable audio |
| `--no-jni` | Disable extended JNI checks |

### Advanced Options

| Option | Description |
|--------|-------------|
| `--grpc <port>` | gRPC port number |
| `--grpc-use-jwt` | Use JWT with gRPC |
| `-v\|--verbose` | Print initialization messages |
| `-h\|--home <path>` | Android SDK home path |

### Examples

```bash
# Start emulator and wait for boot
android avd start --name MyEmulator --wait-boot

# Start headless for CI
android avd start \
  --name CI_Emulator \
  --no-window \
  --no-audio \
  --no-boot-anim \
  --wait-boot \
  --timeout 300

# Start with clean state
android avd start --name MyEmulator --wipe-data --no-snapshot --wait-boot

# Start with extra memory
android avd start --name MyEmulator --memory 4096 --wait-boot

# Start on specific port
android avd start --name MyEmulator --port 5556

# Start and keep running until manually closed
android avd start --name MyEmulator --wait-exit
```

### CI/CD Headless Setup

For running emulators in CI environments without a display:

```bash
# Create emulator
android avd create \
  --name CI_Device \
  --sdk "system-images;android-34;google_apis;x86_64" \
  --device pixel_6 \
  --force

# Start headless with all optimizations
android avd start \
  --name CI_Device \
  --no-window \
  --no-audio \
  --no-boot-anim \
  --no-snapshot \
  --acceleration auto \
  --wait-boot \
  --timeout 300
```

### Troubleshooting

**Slow startup**: Use `--no-boot-anim` and `--no-snapshot-load` for faster cold boots.

**Out of memory**: Reduce `--memory` or close other applications.

**Port conflicts**: Specify a different port with `--port`.

**Headless crashes**: Ensure `--no-window` is used with `--gpu swiftshader_indirect` or `--gpu off` if GPU acceleration isn't available.
