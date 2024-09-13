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

using MovieAutoMerge.Extension;
using MovieAutoMerge.ScheduledTasks.Model;
using MovieAutoMerge.Utils;

namespace MovieAutoMerge.ScheduledTasks
{
    /// <summary>
    /// Merge movies task
    /// </summary>
    public class MergeMoviesTask : IScheduledTask, IConfigurableScheduledTask
    {
        private static bool s_isScanRunning;
        private static readonly BaseItemEqualityComparer BaseItemEqualityComparer = new BaseItemEqualityComparer();
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
        public string Key => nameof(MergeMoviesTask);

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
        public MergeMoviesTask(
            ILibraryManager libraryManager,
            ILogManager logManager,
            IServerConfigurationManager serverConfigurationManager,
            IJsonSerializer jsonSerializer)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger(Plugin.Instance.Name);
            _serverConfigurationManager = serverConfigurationManager;
            _availableTranslations = EmbyHelper.GetAvailableTranslations($"ScheduledTasks.{nameof(MergeMoviesTask)}");
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
            _logger.Info("Start merge movies task");
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

                foreach ((var libraryId, List<BaseItem> movies) in items)
                {
                    Dictionary<string, HashSet<BaseItem>> groups = PrepareItemsForMerge(movies);
                    if (groups.Count == 0)
                    {
                        _logger.Info("Found no movies with ungrouped versions");
                    }
                    else
                    {
                        _logger.Info("Found {0} movies that require regrouping", groups.Count);
                        var libraryName = libraryId != -1
                            ? _libraryManager.GetItemList(new InternalItemsQuery
                            {
                                IncludeItemTypes = new[] { nameof(CollectionFolder) },
                                IsFolder = true,
                                IsVirtualItem = false,
                                ItemIds = new[] { libraryId }
                            })[0].Name
                            : String.Empty;

                        double current = 0.0;
                        foreach (var group in groups)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                _logger.Info("Cancellation was requested");
                                return Task.FromCanceled(cancellationToken);
                            }

                            UpdateCollection(group.Value, libraryName);
                            current++;
                            progress.Report(current / groups.Count);
                        }
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.ErrorException($"Failed to merge movies due to: '{ex.Message}", ex);
                return Task.FromException(ex);
            }
            finally
            {
                s_isScanRunning = false;
                _logger.Info("Task finished");
            }
        }

        private void UpdateCollection(ICollection<BaseItem> set, string libraryName)
        {
            if (set.Count > 0)
            {
                _logger.Info("Updating movie '{0}' {1}with {2} separate versions",
                    set.First().Name,
                    string.IsNullOrEmpty(libraryName) ? string.Empty : $"from {libraryName} ",
                    set.Count);

                _libraryManager.MergeItems(set.ToArray());
                set.Clear();
            }
        }

        private Dictionary<string, HashSet<BaseItem>> PrepareItemsForMerge(IReadOnlyCollection<BaseItem> movies)
        {
            var providerTypes = movies
                .SelectMany(i => i.ProviderIds?.Keys)
                .Distinct()
                .Where(i => !string.IsNullOrWhiteSpace(i));
            _logger.Info("Found {0} different providers: {1}", providerTypes.Count(), string.Join(",", providerTypes));

            List<IGrouping<string, BaseItem>> groups = new List<IGrouping<string, BaseItem>>();
            foreach (var providerType in providerTypes)
            {
                var list = movies
                    .Where(i => i.ProviderIds != null && i.ProviderIds.TryGetValue(providerType, out _))
                    .GroupBy(
                        i => GetMovieKey(providerType, i),
                        i => i)
                    .Where(g => g.Count() != 1 + g.OfType<Video>().Sum(video => video.GetAlternateVersionIds().Count) / g.Count());
                groups.AddRange(list);
            }

            _logger.Info("Found {0} movie groups", groups.Count);

            Dictionary<string, HashSet<BaseItem>> result = new Dictionary<string, HashSet<BaseItem>>();
            foreach (var group in groups)
            {
                var key = group.Key;
                if (!result.TryGetValue(key, out var set))
                {
                    set = new HashSet<BaseItem>(BaseItemEqualityComparer);
                    result.Add(key, set);
                }

                // for each base item in the group
                foreach (var baseItem in group)
                {
                    // add to current set each item from the group
                    set.Add(baseItem);

                    // for each provider id in the item
                    foreach (var subKey in baseItem.ProviderIds
                                 .Select(providerIdKeyPair => GetMovieKey(providerIdKeyPair.Key, baseItem))
                                 .Where(l => !key.Equals(l, StringComparison.Ordinal)))
                    {
                        // fetch existing pair
                        if (result.TryGetValue(subKey, out var subSet))
                        {
                            // merge with the main set
                            set.UnionWith(subSet);
                        }

                        // return main set to the dictionary under additional name
                        result[subKey] = set;
                    }
                }
            }

            return result;
        }

        private static string GetMovieKey(string providerType, IHasProviderIds baseItem)
        {
            return $"{providerType}@{baseItem.GetProviderId(providerType)}";
        }

        private IEnumerable<(long libraryId, List<BaseItem> movies)> GetItemsToProcess()
        {
            var config = Plugin.Instance.Configuration;
            _logger.Info("Choosing items: MergeAcrossLibraries-{0}", config.MergeAcrossLibraries);
            var libraries = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof(CollectionFolder) },
                IsFolder = true,
                IsVirtualItem = false
            });
            _logger.Info("Found {0} libraries. Scanning movies...", libraries.Length);
            if (config.MergeAcrossLibraries)
            {
                var libIds = libraries
                    .Where(l => !"Top Picks".Equals(l.Name, StringComparison.Ordinal))
                    .Select(l => l.InternalId)
                    .ToArray();
                var movies = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    Recursive = true,
                    ParentIds = libIds,
                    IncludeItemTypes = new[] { nameof(Movie) },
                    IsVirtualItem = false,
                    MediaTypes = new[] { nameof(MediaType.Video) },
                    HasPath = true
                });

                List<BaseItem> toReturn = FilterValidMovies(movies, config.DoNotChangeLockedItems);
                _logger.Info("Found {0} applicable movie files in libraries", toReturn.Count);
                yield return (-1, toReturn);
            }
            else
            {
                foreach (var library in libraries)
                {
                    if (library.Name == "Top Picks")
                    {
                        _logger.Info("Ignoring library \"Top Picks\".");
                        continue;
                    }

                    var movies = _libraryManager.GetItemList(new InternalItemsQuery
                    {
                        Recursive = true,
                        ParentIds = new[] { library.InternalId },
                        IncludeItemTypes = new[] { nameof(Movie) },
                        IsVirtualItem = false,
                        MediaTypes = new[] { nameof(MediaType.Video) },
                        HasPath = true
                    });

                    List<BaseItem> toReturn = FilterValidMovies(movies, config.DoNotChangeLockedItems);
                    _logger.Info("Found {0} applicable movie files in library \"{1}\".", toReturn.Count, library.Name);
                    yield return (library.InternalId, toReturn);
                }
            }
        }

        private List<BaseItem> FilterValidMovies(IEnumerable<BaseItem> movies, bool doNotChangeLockedItems)
        {
            List<BaseItem> toReturn = new List<BaseItem>();
            foreach (var movie in movies)
            {
                if (doNotChangeLockedItems && movie.IsLocked)
                {
                    _logger.Info("Ignoring locked item: {0}", movie.Name);
                }
                else if (movie.LocationType == LocationType.FileSystem
                         && movie.GetTopParent() != null
                         && !(movie.Parent.GetParent() is BoxSet))
                {
                    toReturn.Add(movie);
                }
            }

            return toReturn;
        }
    }
}
