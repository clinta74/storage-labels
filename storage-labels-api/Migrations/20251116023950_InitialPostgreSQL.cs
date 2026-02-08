using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StorageLabelsApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        private static readonly string[] columns = new[] { "LocationId", "Code" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commonlocations",
                columns: table => new
                {
                    CommonLocationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commonlocations", x => x.CommonLocationId);
                });

            migrationBuilder.CreateTable(
                name: "images",
                columns: table => new
                {
                    ImageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_images", x => x.ImageId);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    LocationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.LocationId);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Preferences = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "boxes",
                columns: table => new
                {
                    BoxId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImageMetadataId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAccessed = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boxes", x => x.BoxId);
                    table.ForeignKey(
                        name: "FK_boxes_images_ImageMetadataId",
                        column: x => x.ImageMetadataId,
                        principalTable: "images",
                        principalColumn: "ImageId");
                    table.ForeignKey(
                        name: "FK_boxes_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "LocationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userlocations",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userlocations", x => new { x.UserId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_userlocations_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "LocationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_userlocations_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    BoxId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImageMetadataId = table.Column<Guid>(type: "uuid", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_items_boxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "boxes",
                        principalColumn: "BoxId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_items_images_ImageMetadataId",
                        column: x => x.ImageMetadataId,
                        principalTable: "images",
                        principalColumn: "ImageId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_boxes_Code",
                table: "boxes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_boxes_ImageMetadataId",
                table: "boxes",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_boxes_LocationId_Code",
                table: "boxes",
                columns: columns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_BoxId",
                table: "items",
                column: "BoxId");

            migrationBuilder.CreateIndex(
                name: "IX_items_ImageMetadataId",
                table: "items",
                column: "ImageMetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_userlocations_LocationId",
                table: "userlocations",
                column: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commonlocations");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "userlocations");

            migrationBuilder.DropTable(
                name: "boxes");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "images");

            migrationBuilder.DropTable(
                name: "locations");
        }
    }
}
