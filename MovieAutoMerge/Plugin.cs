using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins.UI;
using MediaBrowser.Model.Serialization;

using MovieAutoMerge.I18n;
using MovieAutoMerge.Storage;
using MovieAutoMerge.UI;

namespace MovieAutoMerge
{
    [ExcludeFromCodeCoverage]
    public class Plugin : BasePlugin, IHasThumbImage, IHasUIPages
    {
        internal const string PluginName = "Movie Auto Merge";
        private const string PluginGuidString = "7f6902cc-a3ba-40d9-868f-98f73291fdf7";

        public static Plugin Instance { get; private set; }

        public MainPageUI Options => _pluginOptionsStore.GetOptions();
        public override string Name => PluginName;
        public override string Description => "Auto merge movies based on provider id";
        public override Guid Id => _id;
        private readonly Guid _id = new Guid(PluginGuidString);

        private readonly IServerApplicationHost _applicationHost;
        private readonly ILogger _logger;
        private readonly PluginOptionsStore _pluginOptionsStore;

        public Plugin(IServerApplicationHost applicationHost, ILogManager logManager)
        {
            Instance = this;
            _applicationHost = applicationHost;
            _logger = logManager.GetLogger(PluginName);
            _pluginOptionsStore = new PluginOptionsStore(applicationHost, _logger, PluginName.Replace(" ", string.Empty));

            PluginResource.JsonSerializer = applicationHost.Resolve<IJsonSerializer>();
            PluginResource.ServerConfigurationManager = applicationHost.Resolve<IServerConfigurationManager>();
        }

        #region IHasThumbImage

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public Stream GetThumbImage()
        {
            Type type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        #endregion

        #region IHasUIPages

        private List<IPluginUIPageController> _pages;

        public IReadOnlyCollection<IPluginUIPageController> UIPageControllers
        {
            get
            {
                if (_pages == null)
                {
                    _pages = new List<IPluginUIPageController>
                    {
                        new PluginPageController(_logger, GetPluginInfo(), _applicationHost, _pluginOptionsStore)
                    };
                }

                return _pages.AsReadOnly();
            }
        }

        #endregion
    }
}
