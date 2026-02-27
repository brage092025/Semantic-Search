using Api.Models;

namespace Api.Services;

public interface ISearchService
{
    Task<List<SearchResult>> SearchAsync(SearchRequest request);
}
