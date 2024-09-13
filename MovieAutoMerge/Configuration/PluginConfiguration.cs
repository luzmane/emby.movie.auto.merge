using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Emby.Naming.Common;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Plugins;

namespace MovieAutoMerge.Configuration
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Merge Across Libraries
        /// </summary>
        public bool MergeAcrossLibraries { get; set; } = true;

        /// <summary>
        /// Do Not Change Locked Items
        /// </summary>
        public bool DoNotChangeLockedItems { get; set; } = true;

        /// <summary>
        /// Run movie merge on Library scan or on movie add to a library
        /// </summary>
        public bool RunAutomatically { get; set; } = true;

        /// <summary>
        /// List of providers to choose for specified movie split. Read only from UI
        /// </summary>
        public List<string> ProvidersList
        {
            get
            {
                return Plugin.Instance.LibraryManager.GetItemList(new InternalItemsQuery
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
                    .ToList();
            }
        }
    }
}
