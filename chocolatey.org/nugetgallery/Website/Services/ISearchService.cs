using System.Linq;

namespace NuGetGallery
{
    public interface ISearchService
    {
        /// <summary>
        /// Searches for packages that match the search filter and returns a set of results.
        /// </summary>
        /// <param name="filter">The filter to be used.</param>
        SearchResults Search(SearchFilter filter);

        /// <summary>
        /// Gets a boolean indicating if all versions of each package are stored in the index
        /// </summary>
        bool ContainsAllVersions { get; }
    }
}