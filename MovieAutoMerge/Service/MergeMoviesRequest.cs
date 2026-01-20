using System.Diagnostics.CodeAnalysis;

using MediaBrowser.Model.Services;

using MovieAutoMerge.Service.Models;
using MovieAutoMerge.ScheduledTasks;

namespace MovieAutoMerge.Service
{
    [ExcludeFromCodeCoverage]
    public class MergeMoviesRequest : IService
    {
        private readonly SplitMoviesTask _splitMovies;

        public MergeMoviesRequest(SplitMoviesTask splitMovies)
        {
            _splitMovies = splitMovies;
        }

        public bool Get(SplitRequest request)
        {
            return _splitMovies.SplitMovies(request.ProviderType, request.ProviderId);
        }
    }
}
