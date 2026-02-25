using System.Text.Json;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using OllamaSharp.Models;
using Pgvector;
using System.Linq;
using System.Collections.Generic;

namespace Api.Services;

public class DatabaseSeeder
{
    private readonly DataContext _context;
    private readonly OllamaApiClient _ollama;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(DataContext context, OllamaApiClient ollama, IConfiguration configuration)
    {
        _context = context;
        _ollama = ollama;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        Console.WriteLine("Starting DatabaseSeeder.SeedAsync...");
        // Ensure models are available in Ollama
        var embeddingModel = _configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        var chatModel = _configuration["Ollama:ChatModel"] ?? "gemma3:1b";

        Console.WriteLine($"Checking for models: {embeddingModel}, {chatModel}...");
        try
        {
            Console.WriteLine($"Pulling {embeddingModel}...");
            await foreach (var progress in _ollama.PullModelAsync(embeddingModel))
            {
                if (progress != null && !string.IsNullOrEmpty(progress.Status))
                {
                    Console.WriteLine($"- {embeddingModel}: {progress.Status}");
                }
            }

            Console.WriteLine($"Pulling {chatModel}...");
            await foreach (var progress in _ollama.PullModelAsync(chatModel))
            {
                if (progress != null && !string.IsNullOrEmpty(progress.Status))
                {
                    Console.WriteLine($"- {chatModel}: {progress.Status}");
                }
            }
            Console.WriteLine("Models are ready.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not pull models automatically: {ex.Message}");
        }

        // Clear existing stories to ensure we get the new shorter summaries
        if (await _context.Stories.AnyAsync())
        {
            Console.WriteLine("Clearing existing stories...");
            _context.Stories.RemoveRange(_context.Stories);
            await _context.SaveChangesAsync();
        }

        var currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine($"Current directory: {currentDir}");

        var metadataPath = Path.Combine(currentDir, "..", "Stories", "metadata.json");
        var storiesPath = Path.Combine(currentDir, "..", "Stories");

        // Fallback for different execution contexts
        if (!File.Exists(metadataPath))
        {
            metadataPath = Path.Combine(currentDir, "Stories", "metadata.json");
            storiesPath = Path.Combine(currentDir, "Stories");
        }

        if (!File.Exists(metadataPath))
        {
            Console.WriteLine($"CRITICAL: Metadata not found at {metadataPath}");
            // List files to help debug
            try {
                Console.WriteLine("Files in current directory:");
                foreach(var f in Directory.GetFiles(currentDir)) Console.WriteLine($"  {Path.GetFileName(f)}");
            } catch {}
            return;
        }

        Console.WriteLine($"Found metadata at {metadataPath}. Reading...");
        var metadataJson = await File.ReadAllTextAsync(metadataPath);
        var metadata = JsonSerializer.Deserialize<List<StoryMetadata>>(metadataJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Console.WriteLine($"Loaded metadata for {metadata?.Count ?? 0} stories.");

        _ollama.SelectedModel = chatModel;

        foreach (var meta in metadata!)
        {
            Console.WriteLine($"Processing story: {meta.Title}...");
            // Convert title to filename: "The Lighthouse Keeper" -> "The_Lighthouse_Keeper.txt"
            var sanitizedTitle = new string(meta.Title!
                .Replace("'", "")
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray());
            
            while (sanitizedTitle.Contains("__")) sanitizedTitle = sanitizedTitle.Replace("__", "_");
            sanitizedTitle = sanitizedTitle.Trim('_');

            var files = Directory.GetFiles(storiesPath, "*.txt");
            var fileName = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(sanitizedTitle, StringComparison.OrdinalIgnoreCase));

            if (fileName == null)
            {
                Console.WriteLine($"Story file not found for title: {meta.Title} (Expected {sanitizedTitle}.txt in {storiesPath})");
                continue;
            }

            Console.WriteLine($"Reading content from {Path.GetFileName(fileName)}...");
            var rawContent = await File.ReadAllTextAsync(fileName);
            
            // Remove title and metadata from content (first few lines)
            var lines = rawContent.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            // Typical structure: Title, Author, Year, Genre, [empty line], Content...
            // We want to remove lines until we find the first significant paragraph after potential metadata.
            // Based on files like 'The Garden Of Second Chances', it's:
            // Line 0: Title
            // Line 1: Author
            // Line 2: Year
            // Line 3: Genre
            // Line 4: [Empty]
            // Line 5: First line of story
            int skipLines = 0;
            if (lines.Count > 4 && lines[0] == meta.Title && lines[1] == meta.Author)
            {
                skipLines = 4;
                if (lines.Count > skipLines && string.IsNullOrWhiteSpace(lines[skipLines]))
                {
                    skipLines++;
                }
            }
            var content = string.Join("\n", lines.Skip(skipLines));

            Console.WriteLine("Generating summary...");
            var chat = new Chat(_ollama);
            string summary = "";
            await foreach (var response in chat.SendAsync($"Summarize the following story in exactly one or two sentences: {content}"))
            {
                summary += response;
            }
            
            Console.WriteLine("Generating embedding...");
            var embedRequest = new EmbedRequest { Model = embeddingModel, Input = new List<string> { content } };
            var embeddingResult = await _ollama.EmbedAsync(embedRequest);

            var story = new Story
            {
                Title = meta.Title,
                Author = meta.Author,
                Genre = meta.Genre,
                PublishedYear = meta.PublishedYear,
                Content = content,
                Summary = summary,
                Embedding = new Vector(embeddingResult.Embeddings.FirstOrDefault() ?? [])
            };

            await _context.Stories.AddAsync(story);
            Console.WriteLine($"Story '{meta.Title}' ready to be saved.");
        }

        Console.WriteLine("Saving all stories to database...");
        await _context.SaveChangesAsync();
        Console.WriteLine("Database seeding completed successfully.");
    }
}
