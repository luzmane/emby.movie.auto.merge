using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;

using MovieAutoMerge.I18n;

namespace MovieAutoMerge.ScheduledTasks
{
    public class SplitMoviesTask : IScheduledTask, IConfigurableScheduledTask
    {
        private static bool s_isScanRunning;
        private static readonly object ScanLock = new object();
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        #region Task Config

        public string Name => PluginResource.ResourceManager.GetString("SplitMoviesTask_Name");

        public string Key => nameof(SplitMoviesTask);

        public string Description => PluginResource.ResourceManager.GetString("SplitMoviesTask_Description");

        public string Category => PluginResource.ResourceManager.GetString("PluginTasks_Category");

        public bool IsHidden => false;

        public bool IsEnabled => true;

        public bool IsLogged => true;

        #endregion

        public SplitMoviesTask(ILibraryManager libraryManager, ILogManager logManager)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger(Plugin.PluginName);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("Start split movies task");
            if (s_isScanRunning)
            {
                _logger.Info("The task is running already, exiting");
                _logger.Info("Task finished");
                return Task.CompletedTask;
            }

            lock (ScanLock)
            {
                if (s_isScanRunning)
                {
                    _logger.Info("The task is running already, exiting");
                    _logger.Info("Task finished");
                    return Task.CompletedTask;
                }

                s_isScanRunning = true;
            }

            try
            {
                var items = GetItemsToProcess();

                double current = 0.0;
                foreach (var video in items)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.Info("Cancellation was requested");
                        return Task.FromCanceled(cancellationToken);
                    }

                    if (video.GetAlternateVersionIds().Count > 0)
                    {
                        _libraryManager.SplitItems(video);
                        _logger.Info("Movie '{0}' split", video.Name);
                    }

                    current++;
                    progress.Report(current / items.Count);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"Failed to split movies due to: '{ex.Message}", ex);
                return Task.FromException(ex);
            }
            finally
            {
                s_isScanRunning = false;
                _logger.Info("Task finished");
            }
        }

        /// <summary>
        /// Split specific movie
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="providerId"></param>
        public bool SplitMovies(string providerType, string providerId)
        {
            _logger.Info($"Split {providerType}/{providerId}");
            if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(providerType))
            {
                _logger.Error("Provider type or ID is empty");
                return false;
            }

            var movies = _libraryManager.GetItemList(new InternalItemsQuery
            {
                Recursive = true,
                IncludeItemTypes = new[] { nameof(Movie) },
                IsVirtualItem = false,
                MediaTypes = new[] { nameof(MediaType.Video) },
                HasPath = true,
                AnyProviderIdEquals = new Dictionary<string, string>
                {
                    { providerType, providerId }
                }
            });

            bool toReturn = false;
            foreach (var video in movies.OfType<Video>().Where(v => v.GetAlternateVersionIds().Count > 0))
            {
                _libraryManager.SplitItems(video);
                _logger.Info($"Movie {providerType}/{providerId} split");
                toReturn = true;
            }

            return toReturn;
        }

        private List<Video> GetItemsToProcess()
        {
            var toReturn = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    Recursive = true,
                    IncludeItemTypes = new[] { nameof(Movie) },
                    IsVirtualItem = false,
                    MediaTypes = new[] { nameof(MediaType.Video) },
                    HasPath = true
                })
                .OfType<Video>()
                .Where(movie => movie.LocationType == LocationType.FileSystem
                                && movie.GetTopParent() != null
                                && !(movie.Parent.GetParent() is BoxSet)
                                && movie.GetAlternateVersionIds().Count > 0)
                .ToList();

            if (Plugin.Instance.Options.DoNotChangeLockedItems)
            {
                _logger.Info("Excluding locked items");
                var lockedAltVersions = toReturn
                    .Where(m => m.IsLocked)
                    .SelectMany(m => m.GetAlternateVersionIds());

                toReturn
                    .RemoveAll(m =>
                        m.GetAlternateVersionIds().Any(a => lockedAltVersions.Contains(a))
                        || lockedAltVersions.Contains(m.InternalId));
            }

            _logger.Info("Found {0} applicable movie files in libraries", toReturn.Count);
            return toReturn;
        }
    }
}
