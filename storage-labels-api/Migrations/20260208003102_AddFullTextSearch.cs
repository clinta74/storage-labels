using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace StorageLabelsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "items",
                type: "tsvector",
                nullable: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "boxes",
                type: "tsvector",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "items");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "boxes");
        }
    }
}
