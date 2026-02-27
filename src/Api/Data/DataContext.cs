using Api.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Api.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Story> Stories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Story>(entity =>
        {
            entity.Property(s => s.Embedding)
                .HasColumnType("vector(768)");

            entity.HasIndex(s => s.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");

            // Generated tsvector column for Keyword Search
            entity.HasGeneratedTsVectorColumn(
                    s => s.SearchVector!,
                    "english",
                    s => new { s.Title, s.Author, s.Genre, s.Summary, s.Content })
                .HasIndex(s => s.SearchVector)
                .HasMethod("gin");
        });
    }
}
