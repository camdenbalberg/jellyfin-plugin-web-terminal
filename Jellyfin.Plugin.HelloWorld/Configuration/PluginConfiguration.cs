using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.HelloWorld.Configuration;

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
        CommandTimeoutSeconds = 30;
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
}
