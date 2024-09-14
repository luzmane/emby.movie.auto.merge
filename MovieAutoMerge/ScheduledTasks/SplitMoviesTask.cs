using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;

using MovieAutoMerge.ScheduledTasks.Model;
using MovieAutoMerge.Utils;

namespace MovieAutoMerge.ScheduledTasks
{
    /// <summary>
    /// Split movies task
    /// </summary>
    public class SplitMoviesTask : IScheduledTask, IConfigurableScheduledTask
    {
        private static bool s_isScanRunning;
        private static readonly object ScanLock = new object();
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly Dictionary<string, TaskTranslation> _translations = new Dictionary<string, TaskTranslation>();
        private readonly Dictionary<string, string> _availableTranslations;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IJsonSerializer _jsonSerializer;

        #region Task Config

        /// <inheritdoc />
        public string Name => GetTranslation().Name;

        /// <inheritdoc />
        public string Key => nameof(SplitMoviesTask);

        /// <inheritdoc />
        public string Description => GetTranslation().Description;

        /// <inheritdoc />
        public string Category => GetTranslation().Category;

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        #endregion

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="libraryManager"></param>
        /// <param name="logManager"></param>
        /// <param name="serverConfigurationManager"></param>
        /// <param name="jsonSerializer"></param>
        public SplitMoviesTask(
            ILibraryManager libraryManager,
            ILogManager logManager,
            IServerConfigurationManager serverConfigurationManager,
            IJsonSerializer jsonSerializer)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger(Plugin.Instance.Name);
            _serverConfigurationManager = serverConfigurationManager;
            _availableTranslations = EmbyHelper.GetAvailableTranslations($"ScheduledTasks.{nameof(SplitMoviesTask)}");
            _jsonSerializer = jsonSerializer;
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }

        private TaskTranslation GetTranslation()
        {
            return EmbyHelper.GetTaskTranslation(_translations, _serverConfigurationManager, _jsonSerializer, _availableTranslations);
        }


        /// <inheritdoc />
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
            foreach (Video video in movies.OfType<Video>())
            {
                if (video.GetAlternateVersionIds().Count > 0)
                {
                    _libraryManager.SplitItems(video);
                    _logger.Info($"Movie {providerType}/{providerId} split");
                    toReturn = true;
                }
            }

            return toReturn;
        }

        private List<Video> GetItemsToProcess()
        {
            var config = Plugin.Instance.Configuration;
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

            if (config.DoNotChangeLockedItems)
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
