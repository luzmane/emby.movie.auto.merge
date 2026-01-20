using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Web.GenericEdit.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;
using MovieAutoMerge.I18n;
using MovieAutoMerge.Storage;
using MovieAutoMerge.UIBaseClasses;

namespace MovieAutoMerge.UI
{
    internal class PluginPageController : ControllerBase
    {
        private readonly PluginInfo _pluginInfo;
        private readonly PluginOptionsStore _pluginOptionsStore;
        private readonly ILogger _logger;
        private readonly IServerApplicationHost _applicationHost;
        private readonly ILibraryManager _libraryManager;

        public PluginPageController(
            ILogger logger,
            PluginInfo pluginInfo,
            IServerApplicationHost applicationHost,
            PluginOptionsStore pluginOptionsStore)
            : base(pluginInfo.Id)
        {
            _logger = logger;
            _pluginInfo = pluginInfo;
            _applicationHost = applicationHost;
            _pluginOptionsStore = pluginOptionsStore;
            _libraryManager = applicationHost.Resolve<ILibraryManager>();

            PageInfo = new PluginPageInfo
            {
                Name = Plugin.PluginName,
                EnableInMainMenu = false,
                DisplayName = $"{Plugin.PluginName} Config",
                MenuIcon = "list_alt",
                IsMainConfigPage = true,
            };
        }

        public override PluginPageInfo PageInfo { get; }

        public override Task<IPluginUIView> CreateDefaultPageView() =>
            Task.FromResult<IPluginUIView>(
                new MainPageView(_pluginInfo, _pluginOptionsStore, _logger, _applicationHost));

        public override Task Initialize(CancellationToken token)
        {
            var options = _pluginOptionsStore.GetOptions();

            PopulateLibraries(options);

            PopulateProviders(options);

            options.MergeItemsSection.Caption = PluginResource.ResourceManager.GetString("MainPageUI_Merge_Movies");
            options.SplitMoviesSection.Caption = PluginResource.ResourceManager.GetString("MainPageUI_Split_Movies");
            options.SplitMoviesButton.Caption = PluginResource.ResourceManager.GetString("MainPageUI_Split");
            options.SplitStatusItem.Caption = PluginResource.ResourceManager.GetString("MainPageUI_Movie_Split_Status");
            options.SplitStatusItem.StatusText = PluginResource.ResourceManager.GetString("MainPageUI_Operation_Not_Started_Yet");

            return Task.CompletedTask;
        }

        private void PopulateLibraries(MainPageUI options)
        {
            var libraries = _libraryManager
                .GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { nameof(CollectionFolder) }
                })
                .OfType<CollectionFolder>()
                .Where(l => !"Top Picks".Equals(l.Name, StringComparison.Ordinal))
                .Select(l => new EditorSelectOption
                {
                    Value = l.InternalId.ToString(),
                    Name = l.Name,
                    IsEnabled = true
                })
                .ToList();
            _logger.Info($"Found {libraries.Count} libraries");

            if (options.Libraries == null)
            {
                options.Libraries = libraries;
            }
            else
            {
                options.Libraries.Clear();
                options.Libraries.AddRange(libraries);
            }
        }

        private void PopulateProviders(MainPageUI options)
        {
            var providers = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    Recursive = true,
                    IncludeItemTypes = new[] { nameof(Movie) },
                    IsVirtualItem = false,
                    MediaTypes = new[] { nameof(MediaType.Video) },
                    HasPath = true
                })
                .SelectMany(i => i.ProviderIds?.Keys)
                .Distinct()
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => new EditorSelectOption
                {
                    Value = i,
                    Name = i,
                    IsEnabled = true
                })
                .ToList();

            _logger.Info($"Found {providers.Count} providers");

            if (options.Providers == null)
            {
                options.Providers = providers;
            }
            else
            {
                options.Providers.Clear();
                options.Providers.AddRange(providers);
            }
        }
    }
}
