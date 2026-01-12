using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using AndroidSdk;

namespace AndroidSdk.Mcp.Resources;

/// <summary>
/// MCP resources providing read-only access to Android SDK information.
/// </summary>
[McpServerResourceType]
public class SdkResources
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets the current SDK installation information.
    /// </summary>
    [McpServerResource(UriTemplate = "android://sdk/info")]
    [Description("Current Android SDK installation information including path and available tools.")]
    public static string GetSdkInfo()
    {
        try
        {
            var sdkManager = CreateSdkManager();
            var home = sdkManager.Home?.FullName;

            if (string.IsNullOrEmpty(home))
            {
                return JsonSerializer.Serialize(new
                {
                    installed = false,
                    message = "Android SDK not found. Set ANDROID_HOME environment variable or install the SDK."
                }, JsonOptions);
            }

            return JsonSerializer.Serialize(new
            {
                installed = true,
                home
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                installed = false,
                message = ex.Message
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Gets the list of installed SDK packages.
    /// </summary>
    [McpServerResource(UriTemplate = "android://sdk/packages/installed")]
    [Description("List of currently installed Android SDK packages.")]
    public static string GetInstalledPackages()
    {
        try
        {
            var sdkManager = CreateSdkManager();
            var packages = sdkManager.SdkManager.List();

            var installed = packages.InstalledPackages.Select(p => new
            {
                id = p.Path,
                version = p.Version,
                description = p.Description,
                location = p.Location
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                count = installed.Count,
                packages = installed
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = ex.Message
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Gets the list of available SDK packages.
    /// </summary>
    [McpServerResource(UriTemplate = "android://sdk/packages/available")]
    [Description("List of available Android SDK packages that can be installed.")]
    public static string GetAvailablePackages()
    {
        try
        {
            var sdkManager = CreateSdkManager();
            var packages = sdkManager.SdkManager.List();

            var available = packages.AvailablePackages.Select(p => new
            {
                id = p.Path,
                version = p.Version,
                description = p.Description
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                count = available.Count,
                packages = available
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = ex.Message
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Gets the list of currently connected devices.
    /// </summary>
    [McpServerResource(UriTemplate = "android://devices")]
    [Description("Currently connected Android devices and running emulators.")]
    public static string GetDevices()
    {
        try
        {
            var sdkManager = CreateSdkManager();
            var devices = sdkManager.Adb.GetDevices();

            var deviceList = devices.Select(d => new
            {
                serial = d.Serial,
                model = d.Model,
                product = d.Product,
                device = d.Device,
                isEmulator = d.IsEmulator
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                count = deviceList.Count,
                devices = deviceList
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = ex.Message
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Gets the list of available AVDs.
    /// </summary>
    [McpServerResource(UriTemplate = "android://avds")]
    [Description("Available Android Virtual Devices (AVDs) that can be started.")]
    public static string GetAvds()
    {
        try
        {
            var sdkManager = CreateSdkManager();
            var avds = sdkManager.AvdManager.ListAvds();

            var avdList = avds.Select(a => new
            {
                name = a.Name,
                device = a.Device,
                path = a.Path,
                target = a.Target,
                basedOn = a.BasedOn
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                count = avdList.Count,
                avds = avdList
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = ex.Message
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Gets the list of available AVD device types (hardware profiles).
    /// </summary>
    [McpServerResource(UriTemplate = "android://avd/devices")]
    [Description("Available AVD device types (hardware profiles) that can be used when creating emulators.")]
    public static string GetAvdDeviceTypes()
    {
        try
        {
            var sdkManager = CreateSdkManager();
            var devices = sdkManager.AvdManager.ListDevices();

            var deviceList = devices.Select(d => new
            {
                id = d.Id,
                name = d.Name,
                oem = d.Oem
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                count = deviceList.Count,
                devices = deviceList
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = ex.Message
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Captures a screenshot from a specific device.
    /// </summary>
    [McpServerResource(UriTemplate = "android://devices/{device}/screenshot")]
    [Description("Captures a screenshot from the specified device. Returns base64-encoded PNG image. Note: Clients may cache this resource - use the device_screenshot tool for guaranteed fresh captures.")]
    public static BlobResourceContents GetDeviceScreenshot(string device)
    {
        var sdkManager = CreateSdkManager();
        
        // Verify device exists
        var devices = sdkManager.Adb.GetDevices();
        var targetDevice = devices.FirstOrDefault(d => 
            d.Serial.Equals(device, StringComparison.OrdinalIgnoreCase));
        
        if (targetDevice == null)
        {
            throw new InvalidOperationException($"Device '{device}' not found. Available devices: {string.Join(", ", devices.Select(d => d.Serial))}");
        }

        // Capture to temp file
        var tempPath = Path.Combine(Path.GetTempPath(), $"screenshot_{Guid.NewGuid()}.png");
        try
        {
            sdkManager.Adb.ScreenCapture(new FileInfo(tempPath), device);
            var imageBytes = File.ReadAllBytes(tempPath);
            
            return new BlobResourceContents
            {
                Uri = $"android://devices/{device}/screenshot",
                MimeType = "image/png",
                Blob = Convert.ToBase64String(imageBytes)
            };
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* ignore cleanup errors */ }
        }
    }

    /// <summary>
    /// Pulls a file from a device.
    /// </summary>
    [McpServerResource(UriTemplate = "android://devices/{device}/files/{+path}")]
    [Description("Pulls a file from the specified device at the given path. Returns the file contents. For text files, returns as text; for binary files, returns as base64-encoded blob.")]
    public static ResourceContents PullFile(string device, string path)
    {
        var sdkManager = CreateSdkManager();
        
        // Verify device exists
        var devices = sdkManager.Adb.GetDevices();
        var targetDevice = devices.FirstOrDefault(d => 
            d.Serial.Equals(device, StringComparison.OrdinalIgnoreCase));
        
        if (targetDevice == null)
        {
            throw new InvalidOperationException($"Device '{device}' not found. Available devices: {string.Join(", ", devices.Select(d => d.Serial))}");
        }

        // Ensure path starts with /
        if (!path.StartsWith("/"))
        {
            path = "/" + path;
        }

        // Pull to temp file
        var tempPath = Path.Combine(Path.GetTempPath(), $"adb_pull_{Guid.NewGuid()}{Path.GetExtension(path)}");
        try
        {
            var success = sdkManager.Adb.Pull(new FileInfo(path), new FileInfo(tempPath), device);
            
            if (!success || !File.Exists(tempPath))
            {
                throw new InvalidOperationException($"Failed to pull file '{path}' from device '{device}'. The file may not exist or may not be accessible.");
            }

            var fileBytes = File.ReadAllBytes(tempPath);
            var resourceUri = $"android://devices/{device}/files{path}";
            
            // Try to determine if it's a text file
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var textExtensions = new[] { ".txt", ".xml", ".json", ".log", ".prop", ".cfg", ".conf", ".ini", ".sh", ".md", ".csv", ".html", ".htm", ".css", ".js" };
            
            if (textExtensions.Contains(extension) || IsLikelyTextFile(fileBytes))
            {
                // Return as text
                var text = System.Text.Encoding.UTF8.GetString(fileBytes);
                return new TextResourceContents
                {
                    Uri = resourceUri,
                    MimeType = GetMimeType(extension),
                    Text = text
                };
            }
            else
            {
                // Return as binary blob
                return new BlobResourceContents
                {
                    Uri = resourceUri,
                    MimeType = GetMimeType(extension),
                    Blob = Convert.ToBase64String(fileBytes)
                };
            }
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* ignore cleanup errors */ }
        }
    }

    private static bool IsLikelyTextFile(byte[] bytes)
    {
        if (bytes.Length == 0) return true;
        
        // Check first 8KB for null bytes or high proportion of non-printable chars
        var sampleSize = Math.Min(bytes.Length, 8192);
        var nonPrintable = 0;
        
        for (int i = 0; i < sampleSize; i++)
        {
            var b = bytes[i];
            if (b == 0) return false; // Null byte = binary
            if (b < 32 && b != 9 && b != 10 && b != 13) // Not tab, LF, CR
            {
                nonPrintable++;
            }
        }
        
        // If more than 10% non-printable, probably binary
        return (nonPrintable / (double)sampleSize) < 0.1;
    }

    private static string GetMimeType(string extension)
    {
        return extension switch
        {
            ".txt" => "text/plain",
            ".xml" => "application/xml",
            ".json" => "application/json",
            ".log" => "text/plain",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".apk" => "application/vnd.android.package-archive",
            ".so" => "application/octet-stream",
            ".dex" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }

    private static AndroidSdkManager CreateSdkManager()
    {
        var envHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
        return new AndroidSdkManager(string.IsNullOrEmpty(envHome) ? null : new DirectoryInfo(envHome));
    }
}
