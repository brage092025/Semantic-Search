using System.Text.Json.Serialization;

namespace Api.Models;

public class StoryMetadata
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Genre { get; set; }
    [JsonPropertyName("published_year")]
    public int PublishedYear { get; set; }
}
