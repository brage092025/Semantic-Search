using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using OllamaSharp.Models;
using Pgvector;

namespace Api.Services;

public class DatabaseSeeder
{
    private readonly DataContext _context;
    private readonly OllamaApiClient _ollama;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(DataContext context, OllamaApiClient ollama, IConfiguration configuration, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _ollama = ollama;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting DatabaseSeeder.SeedAsync...");
        
        var embeddingModel = _configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        var chatModel = _configuration["Ollama:ChatModel"] ?? "gemma3:1b";

        try
        {
            await EnsureModelsAvailable(embeddingModel, chatModel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not pull models automatically.");
        }

        var (metadataPath, storiesPath) = GetFilePaths();
        if (!File.Exists(metadataPath))
        {
            _logger.LogError("Metadata not found at {Path}", metadataPath);
            return;
        }

        var metadataJson = await File.ReadAllTextAsync(metadataPath);
        var metadata = JsonSerializer.Deserialize<List<StoryMetadata>>(metadataJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (metadata == null) return;

        var existingStories = await _context.Stories.ToDictionaryAsync(s => s.Title!, s => s);
        var files = Directory.GetFiles(storiesPath, "*.txt");

        _ollama.SelectedModel = chatModel;

        foreach (var meta in metadata)
        {
            var sanitizedTitle = SanitizeTitle(meta.Title!);
            var fileName = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(sanitizedTitle, StringComparison.OrdinalIgnoreCase));

            if (fileName == null)
            {
                _logger.LogWarning("Story file not found for title: {Title}", meta.Title);
                continue;
            }

            var rawContent = await File.ReadAllTextAsync(fileName);
            var content = ParseContent(rawContent, meta);
            var contentHash = ComputeHash(content);

            if (existingStories.TryGetValue(meta.Title!, out var existing))
            {
                if (existing.ContentHash == contentHash)
                {
                    _logger.LogInformation("Story '{Title}' is up to date. Skipping.", meta.Title);
                    continue;
                }
                _logger.LogInformation("Story '{Title}' has changed. Updating...", meta.Title);
                _context.Stories.Remove(existing);
            }

            _logger.LogInformation("Processing new/updated story: {Title}...", meta.Title);
            
            var summary = await GenerateSummaryAsync(content);
            var embedding = await GenerateEmbeddingAsync(content, embeddingModel);

            var story = new Story
            {
                Title = meta.Title,
                Author = meta.Author,
                Genre = meta.Genre,
                PublishedYear = meta.PublishedYear,
                Content = content,
                Summary = summary,
                ContentHash = contentHash,
                Embedding = new Vector(embedding)
            };

            await _context.Stories.AddAsync(story);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Database seeding completed successfully.");
    }

    private async Task EnsureModelsAvailable(string embed, string chat)
    {
        _logger.LogInformation("Ensuring models {Embed} and {Chat} are available...", embed, chat);
        await foreach (var _ in _ollama.PullModelAsync(embed)) { }
        await foreach (var _ in _ollama.PullModelAsync(chat)) { }
    }

    private (string metadata, string stories) GetFilePaths()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var paths = new[] { 
            Path.Combine(currentDir, "..", "Stories"),
            Path.Combine(currentDir, "Stories"),
            Path.Combine(currentDir, "src", "Stories")
        };

        foreach (var p in paths)
        {
            var meta = Path.Combine(p, "metadata.json");
            if (File.Exists(meta)) return (meta, p);
        }

        return (Path.Combine(currentDir, "metadata.json"), currentDir);
    }

    private string SanitizeTitle(string title)
    {
        var sanitized = new string(title.Replace("'", "").Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        while (sanitized.Contains("__")) sanitized = sanitized.Replace("__", "_");
        return sanitized.Trim('_');
    }

    private string ParseContent(string raw, StoryMetadata meta)
    {
        var lines = raw.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        // Skip metadata lines if they match the story title/author
        int skip = 0;
        if (lines.Count > 0 && lines[0].Trim().Equals(meta.Title, StringComparison.OrdinalIgnoreCase)) skip = 1;
        if (lines.Count > skip && lines[skip].Trim().Equals(meta.Author, StringComparison.OrdinalIgnoreCase)) skip++;
        // Skip subsequent empty lines
        while (lines.Count > skip && string.IsNullOrWhiteSpace(lines[skip])) skip++;
        
        return string.Join("\n", lines.Skip(skip)).Trim();
    }

    private string ComputeHash(string text)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    private async Task<string> GenerateSummaryAsync(string content)
    {
        var chat = new Chat(_ollama);
        var summary = new StringBuilder();
        await foreach (var response in chat.SendAsync($"Summarize the following story in exactly one or two sentences: {content}"))
        {
            summary.Append(response);
        }
        return summary.ToString().Trim();
    }

    private async Task<float[]> GenerateEmbeddingAsync(string content, string model)
    {
        var result = await _ollama.EmbedAsync(new EmbedRequest { Model = model, Input = new List<string> { content } });
        return result.Embeddings?.FirstOrDefault() ?? Array.Empty<float>();
    }
}
