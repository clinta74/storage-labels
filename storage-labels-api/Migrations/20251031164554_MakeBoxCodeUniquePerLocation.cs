using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLabelsApi.Migrations
{
    /// <inheritdoc />
    public partial class MakeBoxCodeUniquePerLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Boxes_Code_BoxId",
                table: "Boxes");

            migrationBuilder.DropIndex(
                name: "IX_Boxes_LocationId",
                table: "Boxes");

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_Code",
                table: "Boxes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_LocationId_Code",
                table: "Boxes",
                columns: new[] { "LocationId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Boxes_Code",
                table: "Boxes");

            migrationBuilder.DropIndex(
                name: "IX_Boxes_LocationId_Code",
                table: "Boxes");

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_Code_BoxId",
                table: "Boxes",
                columns: new[] { "Code", "BoxId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_LocationId",
                table: "Boxes",
                column: "LocationId");
        }
    }
}
