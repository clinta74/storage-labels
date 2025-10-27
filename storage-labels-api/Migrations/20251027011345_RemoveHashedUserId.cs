using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLabelsApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHashedUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Images_HashedUserId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "HashedUserId",
                table: "Images");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashedUserId",
                table: "Images",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Images_HashedUserId",
                table: "Images",
                column: "HashedUserId");
        }
    }
}
