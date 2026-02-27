using Api.Data;
using Api.Endpoints;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using Pgvector.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseVector()));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

builder.Services.AddSingleton(new OllamaApiClient(builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434"));
builder.Services.AddTransient<DatabaseSeeder>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created and migrated before anything else
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try 
    {
        logger.LogInformation("Wait for database to be ready and apply migrations...");
        // Re-attempt a few times if the DB is still starting up
        int retries = 5;
        while (retries > 0)
        {
            try
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
                break;
            }
            catch (Exception ex)
            {
                retries--;
                logger.LogWarning("Migration attempt failed ({RetryCount}/5): {ErrorMessage}", 5 - retries, ex.Message);
                if (retries == 0) throw;
                await Task.Delay(2000);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Error applying migrations.");
        // Fallback for extreme cases to at least get the schema
        try 
        {
            logger.LogInformation("Attempting EnsureCreated as fallback...");
            await context.Database.EnsureCreatedAsync();
        }
        catch {}
    }
}

// Run seeding in the background to avoid blocking startup
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapStoryEndpoints();

app.Run();
