using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.HelloWorld.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HelloWorld;

/// <summary>
/// The main plugin entry point.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>
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
        _logger.LogInformation("HelloWorld plugin loaded!");
    }

    /// <inheritdoc />
    public override string Name => "Hello World";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("256a2512-89aa-43f5-bbc1-2157a5647c3a");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }
}
