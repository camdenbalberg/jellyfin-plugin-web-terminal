using System;
using System.Collections.Generic;
using Jellyfin.Plugin.HelloWorld.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HelloWorld;

/// <summary>
/// The main plugin entry point.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _logger = logger;

        if (string.IsNullOrEmpty(Configuration.ApiKey))
        {
            Configuration.ApiKey = PluginConfiguration.GenerateApiKey();
            SaveConfiguration();
            _logger.LogWarning("Web Terminal: Generated new API key. Configure it in plugin settings.");
        }

        _logger.LogInformation("Web Terminal plugin loaded!");
    }

    /// <inheritdoc />
    public override string Name => "Web Terminal";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("256a2512-89aa-43f5-bbc1-2157a5647c3a");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        var prefix = GetType().Namespace;

        return new[]
        {
            new PluginPageInfo
            {
                Name = "web_terminal",
                EmbeddedResourcePath = prefix + ".Pages.terminal.html",
                EnableInMainMenu = true
            },
            new PluginPageInfo
            {
                Name = "web_terminal.js",
                EmbeddedResourcePath = prefix + ".Pages.terminal.js"
            },
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = prefix + ".Pages.config.html"
            },
            new PluginPageInfo
            {
                Name = Name + ".js",
                EmbeddedResourcePath = prefix + ".Pages.config.js"
            }
        };
    }
}
