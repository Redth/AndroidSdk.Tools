using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using AndroidSdk;

namespace AndroidSdk.Mcp.Tools;

/// <summary>
/// MCP tools for Android device and ADB operations.
/// </summary>
[McpServerToolType]
public class DeviceTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Lists connected Android devices and emulators.
    /// </summary>
    [McpServerTool(Name = "device_list")]
    [Description("Lists all connected Android devices and running emulators. Returns device serial numbers, models, and connection info.")]
    public static string ListDevices(
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        var sdkManager = CreateSdkManager(home);
        var devices = sdkManager.Adb.GetDevices();

        var deviceList = devices.Select(d => new
        {
            serial = d.Serial,
            model = d.Model,
            product = d.Product,
            device = d.Device,
            isEmulator = d.IsEmulator
        }).ToList();

        return JsonSerializer.Serialize(new { devices = deviceList }, JsonOptions);
    }

    /// <summary>
    /// Gets properties from a connected device.
    /// </summary>
    [McpServerTool(Name = "device_info")]
    [Description("Gets system properties from a connected Android device. Can filter by specific property names or patterns.")]
    public static string GetDeviceInfo(
        [Description("Device serial number. If not specified, uses the first available device.")] string? device = null,
        [Description("Property names or patterns to include (e.g., 'ro.product.model', 'ro.build.*'). If not specified, returns common properties.")] string[]? properties = null,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        var sdkManager = CreateSdkManager(home);
        var serial = ResolveDeviceSerial(sdkManager, device);

        // Default to common useful properties if none specified
        var propsToGet = properties ?? new[]
        {
            "ro.product.model",
            "ro.product.brand",
            "ro.product.name",
            "ro.build.version.release",
            "ro.build.version.sdk",
            "ro.product.cpu.abi"
        };

        var props = sdkManager.Adb.GetProperties(serial, propsToGet);

        return JsonSerializer.Serialize(new
        {
            device = serial,
            properties = props
        }, JsonOptions);
    }

    /// <summary>
    /// Installs or uninstalls an app on a device.
    /// </summary>
    [McpServerTool(Name = "device_app")]
    [Description("Installs or uninstalls an Android app on a connected device. Use action 'install' with an APK path, or 'uninstall' with a package name.")]
    public static string ManageApp(
        [Description("Action to perform: 'install' or 'uninstall'.")] string action,
        [Description("For install: path to APK file. For uninstall: package name (e.g., 'com.example.app').")] string package,
        [Description("Device serial number. If not specified, uses the first available device.")] string? device = null,
        [Description("For install: replace existing app if installed.")] bool replace = false,
        [Description("For uninstall: keep app data and cache directories.")] bool keepData = false,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required. Use 'install' or 'uninstall'.", nameof(action));

        if (string.IsNullOrWhiteSpace(package))
            throw new ArgumentException("Package (APK path or package name) is required.", nameof(package));

        var sdkManager = CreateSdkManager(home);
        var serial = ResolveDeviceSerial(sdkManager, device);

        switch (action.ToLowerInvariant())
        {
            case "install":
                if (!File.Exists(package))
                    throw new FileNotFoundException($"APK file not found: {package}");

                try
                {
                    sdkManager.Adb.Install(new FileInfo(package), new Adb.AdbInstallOptions { Replace = replace }, serial);
                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        action = "install",
                        package,
                        device = serial,
                        message = $"Successfully installed {Path.GetFileName(package)}"
                    }, JsonOptions);
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        action = "install",
                        package,
                        device = serial,
                        message = ex.Message
                    }, JsonOptions);
                }

            case "uninstall":
                try
                {
                    sdkManager.Adb.Uninstall(package, keepData, serial);
                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        action = "uninstall",
                        package,
                        device = serial,
                        message = $"Successfully uninstalled {package}"
                    }, JsonOptions);
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        action = "uninstall",
                        package,
                        device = serial,
                        message = ex.Message
                    }, JsonOptions);
                }

            default:
                throw new ArgumentException($"Unknown action '{action}'. Use 'install' or 'uninstall'.", nameof(action));
        }
    }

    /// <summary>
    /// Executes a shell command on a device.
    /// </summary>
    [McpServerTool(Name = "device_shell")]
    [Description("Executes a shell command on a connected Android device. Use for running commands like 'ls', 'pm list packages', 'getprop', etc.")]
    public static string ExecuteShell(
        [Description("Shell command to execute on the device.")] string command,
        [Description("Device serial number. If not specified, uses the first available device.")] string? device = null,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command is required.", nameof(command));

        var sdkManager = CreateSdkManager(home);
        var serial = ResolveDeviceSerial(sdkManager, device);

        try
        {
            var output = sdkManager.Adb.Shell(command, serial);
            return JsonSerializer.Serialize(new
            {
                success = true,
                device = serial,
                command,
                output = output
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                device = serial,
                command,
                message = ex.Message
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Captures a screenshot from a device.
    /// </summary>
    [McpServerTool(Name = "device_screenshot")]
    [Description("Captures a screenshot from a connected Android device and saves it to a local file.")]
    public static string CaptureScreenshot(
        [Description("Local file path to save the screenshot (PNG format).")] string outputPath,
        [Description("Device serial number. If not specified, uses the first available device.")] string? device = null,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path is required.", nameof(outputPath));

        var sdkManager = CreateSdkManager(home);
        var serial = ResolveDeviceSerial(sdkManager, device);

        try
        {
            // Ensure directory exists
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            sdkManager.Adb.ScreenCapture(new FileInfo(outputPath), serial);

            return JsonSerializer.Serialize(new
            {
                success = true,
                device = serial,
                path = Path.GetFullPath(outputPath),
                message = "Screenshot captured successfully."
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                device = serial,
                message = ex.Message
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Gets logcat output from a device.
    /// </summary>
    [McpServerTool(Name = "device_logcat")]
    [Description("Gets log output from a connected Android device. Returns recent log lines (dumps current buffer, does not stream).")]
    public static string GetLogcat(
        [Description("Device serial number. If not specified, uses the first available device.")] string? device = null,
        [Description("Maximum number of lines to return (default: 100).")] int maxLines = 100,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        var sdkManager = CreateSdkManager(home);
        var serial = ResolveDeviceSerial(sdkManager, device);

        try
        {
            var logs = sdkManager.Adb.Logcat(new Adb.AdbLogcatOptions(), null, serial);
            
            // Limit output
            if (logs.Count > maxLines)
                logs = logs.TakeLast(maxLines).ToList();

            return JsonSerializer.Serialize(new
            {
                success = true,
                device = serial,
                lineCount = logs.Count,
                logs
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                device = serial,
                message = ex.Message
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Connects or disconnects a device over the network.
    /// </summary>
    [McpServerTool(Name = "adb_connection")]
    [Description("Connects or disconnects an Android device over TCP/IP network. Use action 'connect' with IP address, or 'disconnect' to disconnect.")]
    public static string ManageConnection(
        [Description("Action to perform: 'connect' or 'disconnect'.")] string action,
        [Description("IP address of the device.")] string ip,
        [Description("Port number (default: 5555).")] int port = 5555,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required. Use 'connect' or 'disconnect'.", nameof(action));

        if (string.IsNullOrWhiteSpace(ip))
            throw new ArgumentException("IP address is required.", nameof(ip));

        var sdkManager = CreateSdkManager(home);

        switch (action.ToLowerInvariant())
        {
            case "connect":
                try
                {
                    sdkManager.Adb.Connect(ip, port);
                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        action = "connect",
                        ip,
                        port,
                        message = $"Connected to {ip}:{port}"
                    }, JsonOptions);
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        action = "connect",
                        ip,
                        port,
                        message = ex.Message
                    }, JsonOptions);
                }

            case "disconnect":
                try
                {
                    sdkManager.Adb.Disconnect(ip, port);
                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        action = "disconnect",
                        ip,
                        port,
                        message = $"Disconnected from {ip}:{port}"
                    }, JsonOptions);
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        action = "disconnect",
                        ip,
                        port,
                        message = ex.Message
                    }, JsonOptions);
                }

            default:
                throw new ArgumentException($"Unknown action '{action}'. Use 'connect' or 'disconnect'.", nameof(action));
        }
    }

    private static AndroidSdkManager CreateSdkManager(string? home)
    {
        if (!string.IsNullOrEmpty(home))
        {
            return new AndroidSdkManager(new DirectoryInfo(home));
        }

        var envHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
        return new AndroidSdkManager(string.IsNullOrEmpty(envHome) ? null : new DirectoryInfo(envHome));
    }

    private static string? ResolveDeviceSerial(AndroidSdkManager sdkManager, string? device)
    {
        if (!string.IsNullOrEmpty(device))
            return device;

        // If no device specified, check if there's exactly one device
        var devices = sdkManager.Adb.GetDevices();
        if (devices.Count == 0)
            throw new InvalidOperationException("No devices connected. Connect a device or start an emulator first.");

        if (devices.Count > 1)
            throw new InvalidOperationException($"Multiple devices connected ({string.Join(", ", devices.Select(d => d.Serial))}). Please specify a device serial.");

        return devices[0].Serial;
    }
}
