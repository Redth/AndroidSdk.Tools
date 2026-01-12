using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using AndroidSdk.Apk;

namespace AndroidSdk.Mcp.Tools;

/// <summary>
/// MCP tools for APK analysis.
/// </summary>
[McpServerToolType]
public class ApkTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Gets information from an APK's manifest.
    /// </summary>
    [McpServerTool(Name = "apk_info")]
    [Description("Reads and returns information from an Android APK file's manifest, including package name, version, and SDK requirements.")]
    public static string GetApkInfo(
        [Description("Path to the APK file to analyze.")] string apkPath)
    {
        if (string.IsNullOrWhiteSpace(apkPath))
            throw new ArgumentException("APK path is required.", nameof(apkPath));

        if (!File.Exists(apkPath))
            throw new FileNotFoundException($"APK file not found: {apkPath}");

        try
        {
            var apkReader = new ApkReader(apkPath);
            var manifest = apkReader.ReadManifest();

            return JsonSerializer.Serialize(new
            {
                success = true,
                path = Path.GetFullPath(apkPath),
                packageId = manifest.Manifest?.PackageId,
                versionName = manifest.Manifest?.VersionName,
                versionCode = manifest.Manifest?.VersionCode,
                minSdkVersion = manifest.Manifest?.UsesSdk?.MinSdkVersion,
                targetSdkVersion = manifest.Manifest?.UsesSdk?.TargetSdkVersion
            }, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                success = false,
                path = Path.GetFullPath(apkPath),
                message = ex.Message
            }, JsonOptions);
        }
    }
}
