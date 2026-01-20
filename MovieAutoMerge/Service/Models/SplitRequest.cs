using System.Diagnostics.CodeAnalysis;

using MediaBrowser.Model.Services;

namespace MovieAutoMerge.Service.Models
{
    [ExcludeFromCodeCoverage]
    [Route("/MergeMovies/Split/{ProviderType}/{ProviderId}", "GET", Summary = "Split specific movie")]
    public class SplitRequest : IReturnVoid
    {
        public string ProviderType { get; set; }

        public string ProviderId { get; set; }
    }
}
