using System;
using System.Security.Cryptography;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.WebTerminal.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        ShellPath = "cmd.exe";
        ShellArgs = "/c";
        CommandTimeoutSeconds = 300;
        ApiKey = string.Empty;
    }

    /// <summary>
    /// Gets or sets the shell executable path.
    /// </summary>
    public string ShellPath { get; set; }

    /// <summary>
    /// Gets or sets the shell arguments prefix.
    /// </summary>
    public string ShellArgs { get; set; }

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the API key for terminal access.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// Generates a new random API key.
    /// </summary>
    public static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..32];
    }
}
