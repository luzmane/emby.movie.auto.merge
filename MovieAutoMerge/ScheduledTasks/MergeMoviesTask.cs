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

using MovieAutoMerge.Extension;
using MovieAutoMerge.I18n;

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

        #region Task Config

        public string Name => PluginResource.ResourceManager.GetString("MergeMoviesTask_Name");

        public string Key => nameof(MergeMoviesTask);

        public string Description => PluginResource.ResourceManager.GetString("MergeMoviesTask_Description");

        public string Category => PluginResource.ResourceManager.GetString("PluginTasks_Category");

        public bool IsHidden => false;

        public bool IsEnabled => true;

        public bool IsLogged => true;

        #endregion

        public MergeMoviesTask(ILibraryManager libraryManager, ILogManager logManager)
        {
            _libraryManager = libraryManager;
            _logger = logManager.GetLogger(Plugin.Instance.Name);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }

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

                foreach ((var libraryId, List<Movie> movies) in items)
                {
                    Dictionary<string, HashSet<Movie>> groups = PrepareItemsForMerge(movies);
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
                _logger.ErrorException($"Failed to merge movies due to: '{ex.Message}'", ex);
                return Task.FromException(ex);
            }
            finally
            {
                s_isScanRunning = false;
                _logger.Info("Task finished");
            }
        }

        private void UpdateCollection(ICollection<Movie> set, string libraryName)
        {
            if (set.Count <= 0)
            {
                return;
            }

            _logger.Info("Updating movie '{0}' {1}with {2} separate versions",
                set.First().Name,
                string.IsNullOrEmpty(libraryName) ? string.Empty : $"from {libraryName} ",
                set.Count);

            try
            {
                _libraryManager.MergeItems(set.ToArray<BaseItem>());
            }
            catch (Exception e)
            {
                _logger.Warn("Failed to merge '{0}' {1}due to '{2}'",
                    set.First().Name,
                    string.IsNullOrEmpty(libraryName) ? string.Empty : $"from '{libraryName}' ",
                    e.Message);
            }

            set.Clear();
        }

        private Dictionary<string, HashSet<Movie>> PrepareItemsForMerge(IReadOnlyCollection<Movie> movies)
        {
            var selectedProviders = Plugin.Instance.Options.SelectedProviders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var providerTypes = movies
                .SelectMany(i => i.ProviderIds?.Keys)
                .Distinct()
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();
            _logger.Debug("Found {0} different providers: {1}", providerTypes.Count, string.Join(",", providerTypes));

            if (selectedProviders.Length > 0)
            {
                _logger.Info("Filtering all available providers by chosen list: {0}", Plugin.Instance.Options.SelectedProviders);
                providerTypes = providerTypes
                    .Where(t => selectedProviders.Contains(t))
                    .ToList();
            }

            _logger.Info("Used {0} different providers: {1}", providerTypes.Count, string.Join(",", providerTypes));

            List<IGrouping<string, Movie>> groups = new List<IGrouping<string, Movie>>();
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

            Dictionary<string, HashSet<Movie>> result = new Dictionary<string, HashSet<Movie>>();
            foreach (var group in groups)
            {
                var key = group.Key;
                if (!result.TryGetValue(key, out var set))
                {
                    set = new HashSet<Movie>(BaseItemEqualityComparer);
                    result.Add(key, set);
                }

                // for each base item in the group
                foreach (var baseItem in group)
                {
                    // add to current set each item from the group
                    set.Add(baseItem);

                    // for each provider id in the item
                    foreach (var subKey in GetItemAvailableProviders(providerTypes, baseItem, key))
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

        private static IEnumerable<string> GetItemAvailableProviders(IEnumerable<string> providersList, Movie item, string key)
        {
            var selectedProviders = Plugin.Instance.Options.SelectedProviders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<KeyValuePair<string, string>> list = selectedProviders.Length > 0
                ? item.ProviderIds.Where(providerIdKeyPair => providersList.Contains(providerIdKeyPair.Key))
                : item.ProviderIds;

            return list
                .Select(providerIdKeyPair => GetMovieKey(providerIdKeyPair.Key, item))
                .Where(l => !key.Equals(l, StringComparison.Ordinal));
        }

        private static string GetMovieKey(string providerType, IHasProviderIds baseItem)
        {
            return $"{providerType}@{baseItem.GetProviderId(providerType)}";
        }

        private IEnumerable<(long libraryId, List<Movie> movies)> GetItemsToProcess()
        {
            var selectedLibraries = Plugin.Instance.Options.SelectedLibraries.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            long[] libraryIds;
            if (selectedLibraries.Length == 0)
            {
                libraryIds = _libraryManager.GetItemList(new InternalItemsQuery
                    {
                        IncludeItemTypes = new[] { nameof(CollectionFolder) },
                        IsFolder = true,
                        IsVirtualItem = false
                    })
                    .Where(l => !"Top Picks".Equals(l.Name, StringComparison.Ordinal))
                    .Select(l => l.InternalId)
                    .ToArray();
            }
            else
            {
                libraryIds = selectedLibraries
                    .Select(l => long.TryParse(l, out var libraryId) ? libraryId : -1)
                    .Where(i => i > 0)
                    .Distinct()
                    .ToArray();
            }

            _logger.Info("Found {0} libraries.", libraryIds.Length);
            _logger.Info("Choosing items: MergeAcrossLibraries-{0}", Plugin.Instance.Options.MergeAcrossLibraries);
            if (Plugin.Instance.Options.MergeAcrossLibraries)
            {
                var movies = _libraryManager.GetItemList(new InternalItemsQuery
                    {
                        Recursive = true,
                        AncestorIds = libraryIds,
                        IncludeItemTypes = new[] { nameof(Movie) },
                        IsVirtualItem = false,
                        MediaTypes = new[] { nameof(MediaType.Video) },
                        HasPath = true
                    })
                    .OfType<Movie>();

                List<Movie> toReturn = FilterValidMovies(movies);
                _logger.Info("Found {0} applicable movie files in libraries", toReturn.Count);
                yield return (-1, toReturn);
            }
            else
            {
                foreach (var libraryId in libraryIds)
                {
                    var movies = _libraryManager.GetItemList(new InternalItemsQuery
                        {
                            Recursive = true,
                            AncestorIds = new[] { libraryId },
                            IncludeItemTypes = new[] { nameof(Movie) },
                            IsVirtualItem = false,
                            MediaTypes = new[] { nameof(MediaType.Video) },
                            HasPath = true
                        })
                        .OfType<Movie>();

                    List<Movie> toReturn = FilterValidMovies(movies);
                    _logger.Info("Found {0} applicable movie files in library \"{1}\".", toReturn.Count, libraryId);
                    yield return (libraryId, toReturn);
                }
            }
        }

        private List<Movie> FilterValidMovies(IEnumerable<Movie> movies)
        {
            List<Movie> toReturn = new List<Movie>();
            foreach (var movie in movies)
            {
                if (Plugin.Instance.Options.DoNotChangeLockedItems && movie.IsLocked)
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
