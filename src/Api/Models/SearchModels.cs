using System.Text.Json.Serialization;

namespace Api.Models;

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SearchMode? Mode { get; set; } = SearchMode.Hybrid;
    public int Limit { get; set; } = 5;
}

public class SearchResult
{
    public Story Story { get; set; } = null!;
    public double Score { get; set; }
}

public enum SearchMode
{
    Semantic,
    Keyword,
    Hybrid
}
