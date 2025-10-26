using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLabelsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddImageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ImageMetadataId",
                table: "Items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImageMetadataId",
                table: "Boxes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    ImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HashedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.ImageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_ImageMetadataId",
                table: "Items",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_ImageMetadataId",
                table: "Boxes",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_HashedUserId",
                table: "Images",
                column: "HashedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Boxes_Images_ImageMetadataId",
                table: "Boxes",
                column: "ImageMetadataId",
                principalTable: "Images",
                principalColumn: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Images_ImageMetadataId",
                table: "Items",
                column: "ImageMetadataId",
                principalTable: "Images",
                principalColumn: "ImageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Boxes_Images_ImageMetadataId",
                table: "Boxes");

            migrationBuilder.DropForeignKey(
                name: "FK_Items_Images_ImageMetadataId",
                table: "Items");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Items_ImageMetadataId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Boxes_ImageMetadataId",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ImageMetadataId",
                table: "Boxes");
        }
    }
}
