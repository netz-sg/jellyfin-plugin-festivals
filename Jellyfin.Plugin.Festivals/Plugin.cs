using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Jellyfin.Plugin.Festivals.Configuration;
using Jellyfin.Plugin.Festivals.Data;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Festivals;

/// <summary>
/// The Festivals plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        var dataDir = Path.Combine(applicationPaths.DataPath, "festivals");
        Store = new FestivalStore(dataDir);
    }

    /// <inheritdoc />
    public override string Name => "Festivals";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("b9f7e2a4-3c1d-4e6f-8a2b-1d5c7e9f0a3b");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Gets the festival data store.
    /// </summary>
    public FestivalStore Store { get; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                DisplayName = "Festivals",
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.config.html", GetType().Namespace),
                EnableInMainMenu = true,
                MenuIcon = "festival"
            }
        ];
    }
}
