using System.Diagnostics.CodeAnalysis;

using MediaBrowser.Model.Services;

namespace MovieAutoMerge.Service.Models
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    [Route("/MergeMovies/Split/{ProviderType}/{ProviderId}", "GET", Summary = "Split specific movie")]
    public class SplitRequest : IReturnVoid
    {
        /// <summary>
        /// Provider Name
        /// </summary>
        public string ProviderType { get; set; }

        /// <summary>
        /// Provider ID
        /// </summary>
        public string ProviderId { get; set; }
    }
}
