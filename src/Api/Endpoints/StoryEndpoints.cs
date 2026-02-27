using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        group.MapPost("/search", async ([FromBody] SearchRequest request, ISearchService searchService, ILogger<Program> logger) =>
        {
            try 
            {
                logger.LogInformation("Search Request: Query='{Query}', Mode={Mode}, Limit={Limit}", 
                    request.Query, request.Mode, request.Limit);

                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest("Query cannot be empty.");
                }

                var results = await searchService.SearchAsync(request);
                return Results.Ok(results);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Search error occurred.");
                return Results.Problem(ex.Message);
            }
        });
    }
}
