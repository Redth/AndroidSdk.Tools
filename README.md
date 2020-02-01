# Android.Tools
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

var avd = avds.FirstOrDefault(a => a.Name == "AVD_Name");

// Start the emulator
var emulatorProcess = emu.Start(avd.Name, new EmulatorStartOptions { NoSnapshot = true });

// Wait for the emulator to be in a bootcomplete state, ready to use
emulatorProcess.WaitForBootComplete();
```


## A Complete Example

A common scenario is to create, start, and deploy an apk to an emulator in CI:

```csharp
// Define the emulator image to use, this is an SDK Manager package id
var AVD_SDK_ID = "system-images;android-29;google_apis_playstore;x86_64";

// API Levels we need to install to build our app
var SDK_PACKAGES = new [] { "platforms;android-21", "platforms;android-26", "platforms;android-29" };

// The name of the emulator AVD instance we will create
var AVD_NAME = "CI_Emulator";

var APP_PROJECT = "MyAndroidApp.csproj";
var APP_PACKAGE_NAME = "com.myapp";
var APP_CONFIG = "Release";
var APP_APK = $"{APP_PROJECT}/bin/{APP_CONFIG}/MonoAndroid90/{APP_PACKAGE_NAME}.apk";

// Make sure all of the tools we need are created and installed
var sdkManager = new SdkManager();
var emu = new Emulator();
var adb = new Adb();
var avdManager = new AvdManager();

// Ensure all the SDK components are installed
AndroidSdk.Acquire(sdkManager, adb, emu, avdManager);

// Install the API levels we need for our app
sdkManager.Install(SDK_PACKAGES);

// Build our Xamarin Android App
var p = Process.Start($"msbuild /p:Configuration={APP_CONFIG} {APP_PROJECT}");
p.WaitForExit();

// Make sure the emulator image we want to use is installed
sdkManager.Install(avdPackageId);

// Create an Emulator instance
avdManager.Create(AVD_NAME, AVD_SDK_ID, "pixel", force: true);

// Start the emulator
var emulatorProcess = emu.Start(AVD_NAME, new EmulatorStartOptions { NoSnapshot = true });

// Wait for the emulator to be in a bootcomplete state, ready to use
emulatorProcess.WaitForBootComplete();

// Install the APK we built for our app earlier
adb.Install(APP_APK  emulatorProcess.Serial);

// Launch UI Tests or the app
// TODO: YOUR CODE HERE
// eg: Launch the app we just installed
adb.LaunchApp(APP_PACKAGE_NAME, emulatorProcess.Serial);

// TODO: Run some tests?

// Clean up the emulator
emulatorProcess.Shutdown();
```

