using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using OllamaSharp.Models;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Api.Endpoints;

public static class StoryEndpoints
{
    public static void MapStoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/stories");

        group.MapGet("/", async (DataContext context) =>
        {
            return await context.Stories.ToListAsync();
        });

        group.MapGet("/{id}", async (int id, DataContext context) =>
        {
            return await context.Stories.FindAsync(id)
                is Story story
                ? Results.Ok(story)
                : Results.NotFound();
        });

        group.MapPost("/search", async ([FromBody] SearchRequest request, DataContext context, OllamaApiClient ollama, IConfiguration configuration) =>
        {
            try 
            {
                var query = request.Query ?? string.Empty;
                var mode = request.Mode ?? SearchMode.Hybrid;
                
                Console.WriteLine($"Search Request: Query='{query}', Mode={mode}, Limit={request.Limit}");

                if (string.IsNullOrWhiteSpace(query))
                {
                    return Results.BadRequest("Query cannot be empty.");
                }

                // 1. Semantic Search (Vector)
                Vector? queryVector = null;
                if (mode is SearchMode.Semantic or SearchMode.Hybrid)
                {
                    var embedModel = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
                    var embedRequest = new EmbedRequest { Model = embedModel, Input = new List<string> { query } };
                    
                    var embeddingResult = await ollama.EmbedAsync(embedRequest);
                    var embedding = embeddingResult.Embeddings?.FirstOrDefault();

                    if (embedding == null || embedding.Length == 0)
                    {
                        Console.WriteLine($"Error: Ollama returned empty embedding for model {embedModel}");
                        return Results.Problem("Could not generate embedding. Please ensure Ollama is running and the model is pulled.");
                    }

                    queryVector = new Vector(embedding);
                    Console.WriteLine($"Generated Query Vector. Dimension: {embedding.Length}");
                }

                // 2. Perform Search based on Mode
                if (mode == SearchMode.Keyword)
                {
                    var keywordResults = await context.Stories
                        .Select(s => new SearchResult
                        {
                            Story = s,
                            Score = (double)(EF.Functions.ToTsVector("english", (s.Title ?? "") + " " + (s.Summary ?? "") + " " + (s.Content ?? ""))
                                .Rank(EF.Functions.WebSearchToTsQuery("english", query)) * 100)
                        })
                        .Where(x => x.Score > 0)
                        .OrderByDescending(x => x.Score)
                        .Take(request.Limit)
                        .ToListAsync();
                    
                    return Results.Ok(keywordResults);
                }

                if (mode == SearchMode.Semantic)
                {
                    var semanticResults = await context.Stories
                        .Where(s => s.Embedding != null)
                        .Select(s => new SearchResult
                        {
                            Story = s,
                            Score = (double)((1 - s.Embedding!.CosineDistance(queryVector!)) * 100)
                        })
                        .OrderByDescending(x => x.Score)
                        .Take(request.Limit)
                        .ToListAsync();

                    return Results.Ok(semanticResults);
                }

                // Hybrid Search
                var hybridResults = await context.Stories
                    .Where(s => s.Embedding != null)
                    .Select(s => new
                    {
                        Story = s,
                        SemanticScore = 1 - s.Embedding!.CosineDistance(queryVector!),
                        KeywordScore = (double)EF.Functions.ToTsVector("english", (s.Title ?? "") + " " + (s.Summary ?? "") + " " + (s.Content ?? ""))
                            .Rank(EF.Functions.WebSearchToTsQuery("english", query))
                    })
                    .Select(x => new SearchResult
                    {
                        Story = x.Story,
                        Score = (x.SemanticScore * 0.7 + x.KeywordScore * 0.3) * 100
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(request.Limit)
                    .ToListAsync();

                return Results.Ok(hybridResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Search Error: {ex}");
                return Results.Problem(ex.Message);
            }
        });
    }
}

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
