using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace ProductionSupportCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "document_chunks",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('english'::regconfig, COALESCE(\"Content\", ''::text))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_SearchVector",
                table: "document_chunks",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_document_chunks_SearchVector",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "document_chunks");
        }
    }
}
