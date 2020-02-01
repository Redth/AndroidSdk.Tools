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

// Get the name of an emulator/device
var emulatorName = adb.GetDeviceName(emulator.Serial);

// Use the emulator's serial in other adb calls
// Useful if there's multiple devices connected
var serial = emulator.Serial;

// Push files
adb.Push(new FileInfo("/some/image.png"), new FileInfo("/sdcard/image.png"), serial);

// Pull files
adb.Pull(new FileInfo("/some/log.txt"), new FileInfo("/local/log.txt"), serial);

// Install an apk
adb.Install(new FileInfo("/some/local/app.apk"), serial);

// Uninstall a package
adb.Uninstall("com.some.app", keepDataAndCacheDirs: false, serial);

// Dump logcat lines
List<string> logs = adb.Logcat(serial);

// Execute shell commands
var output = adb.Shell("ls -l", serial);

// Screen capture
adb.ScreenCapture(new FileInfo("/local/place/to/save/screen.png"), serial);
```


## AVD Manager

Ensure the right SDK packages are installed:

```csharp
var avdManager = new AvdManager("/path/to/desired/android_home");

// Download and install bits if necessary
avdManager.Acquire();
```

Create an emulator (AVD) definition:

```csharp
var avdSdkId = "system-images;android-29;google_apis_playstore;x86_64";

// Make sure the emulator image we want to use is installed
sdkManager.Install(avdPackageId);

// Create an Emulator instance
avdManager.Create("AVD_Name", avdSdkId, "pixel", force: true);
```


## Emulator

Ensure the right SDK packages are installed:

```csharp
var emu = new Emulator("/path/to/desired/android_home");

// Download and install bits if necessary
emu.Acquire();
```

Execute Emulator Commands:

```csharp
// Get a list of available emulators
var avds = emu.ListAvds();

var avd = avds.FirstOrDefault(a => a.Name == ");

// Start the emulator
var emulatorProcess = emu.Start(avd.Name, new EmulatorStartOptions { NoSnapshot = true });

// Wait for the emulator to be in a bootcomplete state, ready to use
emulatorProcess.WaitForBootComplete();
```

