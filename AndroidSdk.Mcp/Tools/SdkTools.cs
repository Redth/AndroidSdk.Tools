using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using AndroidSdk;

namespace AndroidSdk.Mcp.Tools;

/// <summary>
/// MCP tools for Android SDK management operations.
/// </summary>
[McpServerToolType]
public class SdkTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets Android SDK environment information including path, version, and installed tools.
    /// </summary>
    [McpServerTool(Name = "sdk_info")]
    [Description("Gets Android SDK environment information including the SDK path, version, and status of installed tools. Use this to verify SDK installation and find the ANDROID_HOME path.")]
    public static string GetSdkInfo(
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        var sdkManager = CreateSdkManager(home);
        
        var info = new SdkInfoResult
        {
            AndroidHome = sdkManager.Home?.FullName,
            IsValid = sdkManager.Home?.Exists == true
        };

        if (info.IsValid)
        {
            // Check for key SDK components
            info.HasPlatformTools = Directory.Exists(Path.Combine(sdkManager.Home!.FullName, "platform-tools"));
            info.HasCmdlineTools = Directory.Exists(Path.Combine(sdkManager.Home!.FullName, "cmdline-tools"));
            info.HasEmulator = Directory.Exists(Path.Combine(sdkManager.Home!.FullName, "emulator"));
            info.HasBuildTools = Directory.Exists(Path.Combine(sdkManager.Home!.FullName, "build-tools"));

            // Try to get sdkmanager version
            try
            {
                info.SdkManagerVersion = sdkManager.SdkManager.GetVersion()?.ToString();
            }
            catch
            {
                // Version check may fail if cmdline-tools not installed
            }

            // Check if SDK can be modified (write permissions)
            try
            {
                info.CanModify = sdkManager.SdkManager.CanModify();
            }
            catch
            {
                info.CanModify = false;
            }
        }

        return JsonSerializer.Serialize(info, JsonOptions);
    }

    /// <summary>
    /// Lists installed and/or available Android SDK packages.
    /// </summary>
    [McpServerTool(Name = "sdk_list")]
    [Description("Lists Android SDK packages. Can show installed packages, available packages, or both. Use this to see what's installed or find packages to install.")]
    public static string ListPackages(
        [Description("Include installed packages in the results.")] bool installed = true,
        [Description("Include available (not yet installed) packages in the results.")] bool available = false,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        var sdkManager = CreateSdkManager(home);
        var list = sdkManager.SdkManager.List();

        var result = new SdkListResult();

        if (installed)
        {
            result.InstalledPackages = list.InstalledPackages
                .Select(p => new PackageInfo
                {
                    Path = p.Path,
                    Version = p.Version,
                    Description = p.Description,
                    Location = p.Location
                })
                .ToList();
        }

        if (available)
        {
            result.AvailablePackages = list.AvailablePackages
                .Select(p => new PackageInfo
                {
                    Path = p.Path,
                    Version = p.Version,
                    Description = p.Description
                })
                .ToList();
        }

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    /// <summary>
    /// Installs or uninstalls Android SDK packages.
    /// </summary>
    [McpServerTool(Name = "sdk_package")]
    [Description("Installs or uninstalls Android SDK packages. Use action 'install' to add packages or 'uninstall' to remove them. Package paths look like 'platform-tools', 'build-tools;34.0.0', 'platforms;android-34', etc.")]
    public static string ManagePackage(
        [Description("Action to perform: 'install' or 'uninstall'.")] string action,
        [Description("Package path(s) to install or uninstall (e.g., 'platform-tools', 'build-tools;34.0.0').")] string[] packages,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null,
        IProgress<ProgressNotificationValue>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required. Use 'install' or 'uninstall'.", nameof(action));

        if (packages == null || packages.Length == 0)
            throw new ArgumentException("At least one package must be specified.", nameof(packages));

        var sdkManager = CreateSdkManager(home);

        var results = new List<PackageActionResult>();
        var totalPackages = packages.Length;

        for (int i = 0; i < packages.Length; i++)
        {
            var package = packages[i];
            
            // Report progress at start of each package
            progress?.Report(new ProgressNotificationValue
            {
                Progress = i,
                Total = totalPackages,
                Message = $"{action}ing package {i + 1}/{totalPackages}: {package}..."
            });

            var result = new PackageActionResult { Package = package, Action = action };
            try
            {
                switch (action.ToLowerInvariant())
                {
                    case "install":
                        sdkManager.SdkManager.Install(package);
                        result.Success = true;
                        result.Message = $"Successfully installed {package}";
                        break;

                    case "uninstall":
                        sdkManager.SdkManager.Uninstall(package);
                        result.Success = true;
                        result.Message = $"Successfully uninstalled {package}";
                        break;

                    default:
                        result.Success = false;
                        result.Message = $"Unknown action '{action}'. Use 'install' or 'uninstall'.";
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }

            results.Add(result);
        }

        // Report completion
        progress?.Report(new ProgressNotificationValue
        {
            Progress = totalPackages,
            Total = totalPackages,
            Message = $"Completed {action} for {totalPackages} package(s)."
        });

        return JsonSerializer.Serialize(new { results }, JsonOptions);
    }

    /// <summary>
    /// Downloads and bootstraps the Android SDK command-line tools.
    /// </summary>
    [McpServerTool(Name = "sdk_download")]
    [Description("Downloads and installs the Android SDK command-line tools to the specified directory. Use this to bootstrap a new SDK installation or update cmdline-tools.")]
    public static async Task<string> DownloadSdk(
        [Description("Directory to install the Android SDK. This will become ANDROID_HOME.")] string home,
        [Description("Force download even if cmdline-tools already exist.")] bool force = false,
        IProgress<ProgressNotificationValue>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(home))
            throw new ArgumentException("Home directory is required.", nameof(home));

        var homeDir = new DirectoryInfo(home);
        var cmdlineToolsPath = Path.Combine(home, "cmdline-tools");

        if (!force && Directory.Exists(cmdlineToolsPath))
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = "SDK cmdline-tools already exist. Use force=true to re-download.",
                path = home
            }, JsonOptions);
        }

        var sdkManager = new SdkManager(new SdkManagerToolOptions { AndroidSdkHome = homeDir });

        try
        {
            // Report starting download
            progress?.Report(new ProgressNotificationValue
            {
                Progress = 0,
                Total = 100,
                Message = "Starting Android SDK download..."
            });

            // Create a progress handler that forwards to the MCP progress
            Action<int>? downloadProgress = null;
            if (progress != null)
            {
                downloadProgress = (percent) =>
                {
                    progress.Report(new ProgressNotificationValue
                    {
                        Progress = percent,
                        Total = 100,
                        Message = percent < 100 
                            ? $"Downloading Android SDK cmdline-tools... {percent}%"
                            : "Download complete, extracting..."
                    });
                };
            }

            await sdkManager.DownloadSdk(homeDir, null, downloadProgress);

            progress?.Report(new ProgressNotificationValue
            {
                Progress = 100,
                Total = 100,
                Message = "Android SDK download completed."
            });

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = "Successfully downloaded Android SDK cmdline-tools.",
                path = home
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                message = ex.Message,
                path = home
            }, JsonOptions);
        }
    }

    /// <summary>
    /// Lists or accepts Android SDK licenses.
    /// </summary>
    [McpServerTool(Name = "sdk_licenses")]
    [Description("Lists or accepts Android SDK licenses. Use action 'list' to see license status, or 'accept' to accept all pending licenses.")]
    public static string ManageLicenses(
        [Description("Action to perform: 'list' to show licenses or 'accept' to accept all.")] string action = "list",
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        var sdkManager = CreateSdkManager(home);

        switch (action.ToLowerInvariant())
        {
            case "list":
                var licenses = sdkManager.SdkManager.GetLicenses();
                var licenseResults = licenses.Select(l => new
                {
                    id = l.Id,
                    accepted = l.Accepted
                }).ToList();
                return JsonSerializer.Serialize(new { licenses = licenseResults }, JsonOptions);

            case "accept":
                try
                {
                    sdkManager.SdkManager.AcceptLicenses();
                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        message = "All licenses accepted."
                    }, JsonOptions);
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = ex.Message
                    }, JsonOptions);
                }

            default:
                throw new ArgumentException($"Unknown action '{action}'. Use 'list' or 'accept'.", nameof(action));
        }
    }

    private static AndroidSdkManager CreateSdkManager(string? home)
    {
        if (!string.IsNullOrEmpty(home))
        {
            return new AndroidSdkManager(new DirectoryInfo(home));
        }

        // Fall back to environment or auto-detection
        var envHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
        return new AndroidSdkManager(string.IsNullOrEmpty(envHome) ? null : new DirectoryInfo(envHome));
    }

    #region Result Types

    private class SdkInfoResult
    {
        public string? AndroidHome { get; set; }
        public bool IsValid { get; set; }
        public bool HasPlatformTools { get; set; }
        public bool HasCmdlineTools { get; set; }
        public bool HasEmulator { get; set; }
        public bool HasBuildTools { get; set; }
        public string? SdkManagerVersion { get; set; }
        public bool CanModify { get; set; }
    }

    private class SdkListResult
    {
        public List<PackageInfo>? InstalledPackages { get; set; }
        public List<PackageInfo>? AvailablePackages { get; set; }
    }

    private class PackageInfo
    {
        public string? Path { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
    }

    private class PackageActionResult
    {
        public string? Package { get; set; }
        public string? Action { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    #endregion
}
