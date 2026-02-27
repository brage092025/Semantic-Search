using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SearchOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Stories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Stories",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "Author", "Genre", "Summary", "Content" });

            migrationBuilder.CreateIndex(
                name: "IX_Stories_SearchVector",
                table: "Stories",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stories_SearchVector",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Stories");
        }
    }
}
