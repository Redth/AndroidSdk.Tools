using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using AndroidSdk;

namespace AndroidSdk.Mcp.Tools;

/// <summary>
/// MCP tools for JDK discovery and information.
/// </summary>
[McpServerToolType]
public class JdkTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Lists installed JDKs and finds the best one for Android development.
    /// </summary>
    [McpServerTool(Name = "jdk_info")]
    [Description("Lists installed JDK installations and finds the best one for Android development. Optionally filter by a specific major version.")]
    public static string GetJdkInfo(
        [Description("Filter to find JDKs matching a specific major version (e.g., 17, 21).")] int? version = null)
    {
        var locator = new JdkLocator();
        var allJdks = locator.LocateJdk().ToList();

        // Helper to parse major version from version string like "17.0.1", "21", "11.0.2+9"
        static int? GetMajorVersion(string? versionStr)
        {
            if (string.IsNullOrEmpty(versionStr))
                return null;
            
            // Handle versions like "17.0.1", "21", "11.0.2+9", "1.8.0_292"
            var parts = versionStr.Split('.', '+', '_', '-');
            if (parts.Length > 0 && int.TryParse(parts[0], out var major))
            {
                // Java 8 and earlier use "1.x" versioning
                if (major == 1 && parts.Length > 1 && int.TryParse(parts[1], out var minor))
                    return minor;
                return major;
            }
            return null;
        }

        // If version specified, filter and find best match
        if (version.HasValue)
        {
            var matchingJdks = allJdks
                .Where(j => GetMajorVersion(j.Version) == version.Value)
                .ToList();

            var jdkList = matchingJdks.Select(j => new
            {
                home = j.Home.FullName,
                version = j.Version,
                majorVersion = GetMajorVersion(j.Version)
            }).ToList();

            return JsonSerializer.Serialize(new
            {
                requestedVersion = version.Value,
                recommendedHome = matchingJdks.FirstOrDefault()?.Home.FullName,
                jdks = jdkList
            }, JsonOptions);
        }

        // Return all JDKs, sorted by version descending
        var sortedJdks = allJdks
            .OrderByDescending(j => GetMajorVersion(j.Version) ?? 0)
            .ToList();

        // Find the best JDK for Android (prefer JDK 17+ for modern Android)
        var bestForAndroid = sortedJdks
            .Where(j => GetMajorVersion(j.Version) >= 17)
            .FirstOrDefault() ?? sortedJdks.FirstOrDefault();

        var allJdkList = sortedJdks.Select(j => new
        {
            home = j.Home.FullName,
            version = j.Version,
            majorVersion = GetMajorVersion(j.Version)
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            recommendedHome = bestForAndroid?.Home.FullName,
            recommendedVersion = bestForAndroid?.Version,
            jdks = allJdkList
        }, JsonOptions);
    }
}
