using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

using MovieAutoMerge.ScheduledTasks;

namespace MovieAutoMerge
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public sealed class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="libraryManager"></param>
        /// <param name="logManager"></param>
        /// <param name="taskManager"></param>
        public ServerEntryPoint(ILibraryManager libraryManager, ILogManager logManager, ITaskManager taskManager)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger(Plugin.Instance.Name);
            _taskManager = taskManager;
        }


        /// <inheritdoc />
        public void Dispose()
        {
            _libraryManager.ItemAdded -= libraryManager_ItemAdded;
        }

        /// <inheritdoc />
        public void Run()
        {
            _libraryManager.ItemAdded += libraryManager_ItemAdded;
        }

        private async void libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            BaseItem item = e.Item;
            if (nameof(Movie).Equals(item.GetType().Name, StringComparison.Ordinal))
            {
                if (!Plugin.Instance.Configuration.RunAutomatically)
                {
                    _logger.Info("Auto run is turned off");
                    return;
                }

                _logger.Info("Movie added, waiting 1 min to start merge check");
                await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
                try
                {
                    await _taskManager
                        .Execute(_taskManager.ScheduledTasks.FirstOrDefault(t => t.ScheduledTask is MergeMoviesTask), new TaskOptions())
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException(ex.Message, ex);
                }
            }
        }
    }
}
