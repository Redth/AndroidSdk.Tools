using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using AndroidSdk;

namespace AndroidSdk.Mcp.Tools;

/// <summary>
/// MCP tools for Android Virtual Device (AVD) management.
/// </summary>
[McpServerToolType]
public class AvdTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Lists AVDs, available targets, or device definitions.
    /// </summary>
    [McpServerTool(Name = "avd_list")]
    [Description("Lists Android Virtual Devices (AVDs), available system image targets, or device definitions. Use type 'avds' for existing emulators, 'targets' for installable system images, or 'devices' for hardware profiles like 'pixel'.")]
    public static string ListAvd(
        [Description("What to list: 'avds' (existing emulators), 'targets' (system images), or 'devices' (hardware profiles).")] string type = "avds",
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        var sdkManager = CreateSdkManager(home);

        switch (type.ToLowerInvariant())
        {
            case "avds":
                var avds = sdkManager.AvdManager.ListAvds();
                var avdList = avds.Select(a => new
                {
                    name = a.Name,
                    device = a.Device,
                    path = a.Path,
                    target = a.Target,
                    basedOn = a.BasedOn
                }).ToList();
                return JsonSerializer.Serialize(new { avds = avdList }, JsonOptions);

            case "targets":
                var targets = sdkManager.AvdManager.ListTargets();
                var targetList = targets.Select(t => new
                {
                    id = t.Id,
                    numericId = t.NumericId,
                    name = t.Name,
                    type = t.Type,
                    apiLevel = t.ApiLevel,
                    revision = t.Revision
                }).ToList();
                return JsonSerializer.Serialize(new { targets = targetList }, JsonOptions);

            case "devices":
                var devices = sdkManager.AvdManager.ListDevices();
                var deviceList = devices.Select(d => new
                {
                    id = d.Id,
                    name = d.Name,
                    oem = d.Oem
                }).ToList();
                return JsonSerializer.Serialize(new { devices = deviceList }, JsonOptions);

            default:
                throw new ArgumentException($"Unknown type '{type}'. Use 'avds', 'targets', or 'devices'.", nameof(type));
        }
    }

    /// <summary>
    /// Creates or deletes an Android Virtual Device (AVD).
    /// </summary>
    [McpServerTool(Name = "avd_manage")]
    [Description("Creates or deletes an Android Virtual Device (AVD). Use action 'create' with a system image SDK path to create a new emulator, or 'delete' to remove an existing one.")]
    public static string ManageAvd(
        [Description("Action to perform: 'create' or 'delete'.")] string action,
        [Description("Name for the AVD (used to identify and start the emulator).")] string name,
        [Description("System image SDK path for create (e.g., 'system-images;android-34;google_apis;x86_64'). Not needed for delete.")] string? sdk = null,
        [Description("Device definition for create (e.g., 'pixel', 'pixel_7_pro'). Optional.")] string? device = null,
        [Description("Force overwrite if AVD already exists (for create action).")] bool force = false,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required. Use 'create' or 'delete'.", nameof(action));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("AVD name is required.", nameof(name));

        var sdkManager = CreateSdkManager(home);

        switch (action.ToLowerInvariant())
        {
            case "create":
                if (string.IsNullOrWhiteSpace(sdk))
                    throw new ArgumentException("SDK system image path is required for create action (e.g., 'system-images;android-34;google_apis;x86_64').", nameof(sdk));

                try
                {
                    sdkManager.AvdManager.Create(name, sdk, new AvdManager.AvdCreateOptions
                    {
                        Device = device,
                        Force = force
                    });

                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        action = "create",
                        name,
                        message = $"Successfully created AVD '{name}'."
                    }, JsonOptions);
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        action = "create",
                        name,
                        message = ex.Message
                    }, JsonOptions);
                }

            case "delete":
                try
                {
                    sdkManager.AvdManager.Delete(name);
                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        action = "delete",
                        name,
                        message = $"Successfully deleted AVD '{name}'."
                    }, JsonOptions);
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        action = "delete",
                        name,
                        message = ex.Message
                    }, JsonOptions);
                }

            default:
                throw new ArgumentException($"Unknown action '{action}'. Use 'create' or 'delete'.", nameof(action));
        }
    }

    /// <summary>
    /// Starts an Android emulator.
    /// </summary>
    [McpServerTool(Name = "avd_start")]
    [Description("Starts an Android emulator by AVD name. Can optionally wait for the emulator to fully boot before returning.")]
    public static string StartAvd(
        [Description("Name of the AVD to start.")] string name,
        [Description("Wait for the emulator to fully boot before returning.")] bool waitBoot = false,
        [Description("Start without loading/saving snapshots (cold boot).")] bool noSnapshot = false,
        [Description("Run emulator without a visible window (headless mode).")] bool noWindow = false,
        [Description("Android SDK home path. If not specified, auto-detects from environment.")] string? home = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("AVD name is required.", nameof(name));

        var sdkManager = CreateSdkManager(home);

        try
        {
            var options = new Emulator.EmulatorStartOptions
            {
                NoSnapshot = noSnapshot,
                NoWindow = noWindow
            };

            var process = sdkManager.Emulator.Start(name, options);

            if (waitBoot)
            {
                process.WaitForBootComplete();
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                name,
                serial = process.Serial,
                message = waitBoot 
                    ? $"Emulator '{name}' started and boot completed."
                    : $"Emulator '{name}' starting. Use device_list to check status."
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                name,
                message = ex.Message
            }, JsonOptions);
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
}
