using System;
using System.Threading.Tasks;

using Emby.Web.GenericEdit.Elements;

using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Tasks;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;

using MovieAutoMerge.I18n;
using MovieAutoMerge.ScheduledTasks;
using MovieAutoMerge.Storage;
using MovieAutoMerge.UIBaseClasses.Views;

namespace MovieAutoMerge.UI
{
    internal class MainPageView : PluginPageView
    {
        private readonly PluginOptionsStore _pluginOptionsStore;
        private readonly ILogger _logger;
        private readonly SplitMoviesTask _splitMoviesTask;

        public MainPageView(PluginInfo pluginInfo, PluginOptionsStore pluginOptionsStore, ILogger logger, IServerApplicationHost applicationHost)
            : base(pluginInfo.Id)
        {
            _pluginOptionsStore = pluginOptionsStore;
            ContentData = _pluginOptionsStore.GetOptions();
            _logger = logger;

            ILibraryManager libraryManager = applicationHost.Resolve<ILibraryManager>();
            ILogManager logManager = applicationHost.Resolve<ILogManager>();
            _splitMoviesTask = new SplitMoviesTask(libraryManager, logManager);
        }

        public MainPageUI MainPageUI => this.ContentData as MainPageUI;

        public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data)
        {
            _pluginOptionsStore.SetOptions(MainPageUI);

            return base.OnSaveCommand(itemId, commandId, data);
        }

        public override Task<IPluginUIView> RunCommand(string itemId, string commandId, string data)
        {
            switch (commandId)
            {
                case MainPageUI.SplitButtonId:
                    Task.Run(this.HandleSplitMovieButton).FireAndForget(new NullLogger());
                    return Task.FromResult<IPluginUIView>(this);
            }

            return base.RunCommand(itemId, commandId, data);
        }

        private Task HandleSplitMovieButton()
        {
            var providerType = MainPageUI.SplitProviderType;
            var providerId = MainPageUI.SplitProviderId;
            if (string.IsNullOrWhiteSpace(providerType) || string.IsNullOrWhiteSpace(providerId))
            {
                MainPageUI.SplitStatusItem.StatusText = PluginResource.ResourceManager.GetString("MainPageUI_Provider_Name_And_Provider_Id_Are_Required");
                MainPageUI.SplitStatusItem.Status = ItemStatus.Failed;
                RaiseUIViewInfoChanged();

                return Task.CompletedTask;
            }

            MainPageUI.SplitMoviesButton.IsEnabled = false;
            MainPageUI.SplitStatusItem.StatusText = PluginResource.ResourceManager.GetString("MainPageUI_Work_In_Progress");
            MainPageUI.SplitStatusItem.Status = ItemStatus.InProgress;
            RaiseUIViewInfoChanged();

            try
            {
                var result = _splitMoviesTask.SplitMovies(providerType.Trim(), providerId.Trim());
                if (result)
                {
                    MainPageUI.SplitStatusItem.StatusText = PluginResource.ResourceManager.GetString("MainPageUI_Operation_Completed_Successfully");
                    MainPageUI.SplitStatusItem.Status = ItemStatus.Succeeded;
                }
                else
                {
                    MainPageUI.SplitStatusItem.StatusText = PluginResource.ResourceManager.GetString("MainPageUI_Such_Item_Not_Found");
                    MainPageUI.SplitStatusItem.Status = ItemStatus.Failed;
                }
            }
            catch (Exception ex)
            {
                MainPageUI.SplitStatusItem.StatusText = ex.Message;
                MainPageUI.SplitStatusItem.Status = ItemStatus.Succeeded;
                _logger.ErrorException(ex.Message, ex);
            }
            finally
            {
                MainPageUI.SplitMoviesButton.IsEnabled = true;
                RaiseUIViewInfoChanged();
            }

            return Task.CompletedTask;
        }
    }
}
