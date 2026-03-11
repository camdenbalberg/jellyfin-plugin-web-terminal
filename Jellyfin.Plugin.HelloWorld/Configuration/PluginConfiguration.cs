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
        Greeting = "Hello from Jellyfin!";
    }

    /// <summary>
    /// Gets or sets the greeting message.
    /// </summary>
    public string Greeting { get; set; }
}
