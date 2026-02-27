using System.Text.Json.Serialization;
using Pgvector;
using NpgsqlTypes;

namespace Api.Models;

public class Story
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Genre { get; set; }
    public int PublishedYear { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? ContentHash { get; set; }

    [JsonIgnore]
    public Vector? Embedding { get; set; }

    [JsonIgnore]
    public NpgsqlTsVector? SearchVector { get; set; }
}
