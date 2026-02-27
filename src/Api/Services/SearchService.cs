using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using OllamaSharp.Models;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Api.Services;

public class SearchService : ISearchService
{
    private readonly DataContext _context;
    private readonly OllamaApiClient _ollama;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        DataContext context, 
        OllamaApiClient ollama, 
        IConfiguration configuration,
        ILogger<SearchService> logger)
    {
        _context = context;
        _ollama = ollama;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<SearchResult>> SearchAsync(SearchRequest request)
    {
        var query = request.Query;
        var mode = request.Mode ?? SearchMode.Hybrid;
        var limit = request.Limit;

        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<SearchResult>();
        }

        return mode switch
        {
            SearchMode.Keyword => await KeywordSearchAsync(query, limit),
            SearchMode.Semantic => await SemanticSearchAsync(query, limit),
            SearchMode.Hybrid => await HybridSearchAsync(query, limit),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private async Task<List<SearchResult>> KeywordSearchAsync(string query, int limit)
    {
        // Using the optimized SearchVector column with GIN index
        return await _context.Stories
            .Where(s => s.SearchVector!.Matches(EF.Functions.WebSearchToTsQuery("english", query)))
            .Select(s => new SearchResult
            {
                Story = s,
                Score = (double)s.SearchVector!.Rank(EF.Functions.WebSearchToTsQuery("english", query))
            })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToListAsync();
    }

    private async Task<List<SearchResult>> SemanticSearchAsync(string query, int limit)
    {
        var queryVector = await GetEmbeddingAsync(query);
        if (queryVector == null) return new List<SearchResult>();

        return await _context.Stories
            .Where(s => s.Embedding != null)
            .Select(s => new SearchResult
            {
                Story = s,
                Score = (double)(1 - s.Embedding!.CosineDistance(queryVector))
            })
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToListAsync();
    }

    private async Task<List<SearchResult>> HybridSearchAsync(string query, int limit)
    {
        // Get top results from both methods
        var keywordResults = await KeywordSearchAsync(query, limit * 2);
        var semanticResults = await SemanticSearchAsync(query, limit * 2);

        // Reciprocal Rank Fusion (RRF)
        // RRF score = sum(1 / (k + rank))
        const int k = 60; // Standard constant for RRF
        var scores = new Dictionary<int, double>();
        var storyMap = new Dictionary<int, Story>();

        for (int i = 0; i < keywordResults.Count; i++)
        {
            var res = keywordResults[i];
            var id = res.Story.Id;
            storyMap[id] = res.Story;
            scores[id] = 1.0 / (k + i + 1);
        }

        for (int i = 0; i < semanticResults.Count; i++)
        {
            var res = semanticResults[i];
            var id = res.Story.Id;
            storyMap[id] = res.Story;
            
            if (scores.ContainsKey(id))
                scores[id] += 1.0 / (k + i + 1);
            else
                scores[id] = 1.0 / (k + i + 1);
        }

        return scores
            .OrderByDescending(x => x.Value)
            .Take(limit)
            .Select(x => new SearchResult
            {
                Story = storyMap[x.Key],
                Score = x.Value * 100 // Scale for better visibility
            })
            .ToList();
    }

    private async Task<Vector?> GetEmbeddingAsync(string text)
    {
        try
        {
            var model = _configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
            var result = await _ollama.EmbedAsync(new EmbedRequest { Model = model, Input = new List<string> { text } });
            var embedding = result.Embeddings?.FirstOrDefault();
            return embedding != null ? new Vector(embedding) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            return null;
        }
    }
}
