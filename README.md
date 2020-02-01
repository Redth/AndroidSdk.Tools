# Android.Tool
Global dotnet tool for various android adb, avd, and emulator needs.

## Usage

### SDK Manager:

Download/Install the SDK:

```csharp
var sdkManager = new SdkManager("/path/to/desired/android_home");
sdkManager.Acquire();
```

List packages that can be installed:

```csharp
var list = sdkManager.List();

foreach (var a in list.AvailablePackages)
    Console.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");

foreach (var a in list.InstalledPackages)
    Console.WriteLine($"{a.Description}\t{a.Version}\t{a.Path}");
```

Install a particular package:

```csharp
// The `SdkPackage.Path` is used to specify the package to install
// it might look something like: build-tools;29.0.3
var installPath = list.AvailablePackages.FirstOrDefault().Path;

sdkManager.Install(installPath);
```


### ADB (Android Debug Bridge)

Ensure the right SDK packages are installed:

```csharp
var adb = new SdkManager("/path/to/desired/android_home");

// Download and install bits if necessary
adb.Acquire();
```

Execute ADB Commands:

```csharp

// Stop/start adb
adb.KillServer();
adb.StartServer();

// Find all ADB attached devices
var devices = adb.GetDevices();
foreach (var device in devices)
    Console.WriteLine($"ADB Device: {device.Serial}");

// Find an emulator
var emulator = devices.FirstOrDefault(d => d.IsEmulator);

// Get it's name
var emulatorName = adb.GetAvdName(emulator.Serial);

// Set our Adb instance's Serial for subsequent commands
// Useful if there's multiple devices connected
adb.AdbSerial = emulator.Serial;

// Push files
adb.Push(new FileInfo("/some/image.png"), new FileInfo("/sdcard/image.png"));

// Pull files
adb.Pull(new FileInfo("/some/log.txt"), new FileInfo("/local/log.txt"));

// Install an apk
adb.Install(new FileInfo("/some/local/app.apk"));

// Uninstall a package
adb.Uninstall("com.some.app", keepDataAndCacheDirs: false);

// Dump logcat lines
List<string> logs = adb.Logcat();

// Execute shell commands
var output = adb.Shell("ls -l");

// Screen capture
adb.ScreenCapture(new FileInfo("/local/place/to/save/screen.png"));
```


## AVD Manager

Docs coming soon...


## Emulator

Docs coming soon...
