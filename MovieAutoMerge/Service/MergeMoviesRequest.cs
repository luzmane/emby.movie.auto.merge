using System.Diagnostics.CodeAnalysis;

using MediaBrowser.Model.Services;

using MovieAutoMerge.Service.Models;
using MovieAutoMerge.ScheduledTasks;

namespace MovieAutoMerge.Service
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public class MergeMoviesRequest : IService
    {
        private readonly SplitMoviesTask _splitMovies;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="splitMovies"></param>
        public MergeMoviesRequest(SplitMoviesTask splitMovies)
        {
            _splitMovies = splitMovies;
        }

        /// <summary>
        /// Request to split movie by Provide type and ID
        /// </summary>
        /// <param name="request"></param>
        public bool Get(SplitRequest request)
        {
            return _splitMovies.SplitMovies(request.ProviderType, request.ProviderId);
        }
    }
}
