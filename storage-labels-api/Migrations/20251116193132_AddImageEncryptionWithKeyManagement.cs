using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StorageLabelsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddImageEncryptionWithKeyManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "AuthenticationTag",
                table: "images",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EncryptionKeyId",
                table: "images",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "InitializationVector",
                table: "images",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEncrypted",
                table: "images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "encryptionkeys",
                columns: table => new
                {
                    Kid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    KeyMaterial = table.Column<byte[]>(type: "bytea", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DeprecatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Algorithm = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encryptionkeys", x => x.Kid);
                });

            migrationBuilder.CreateTable(
                name: "encryptionkeyrotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromKeyId = table.Column<int>(type: "integer", nullable: false),
                    ToKeyId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalImages = table.Column<int>(type: "integer", nullable: false),
                    ProcessedImages = table.Column<int>(type: "integer", nullable: false),
                    FailedImages = table.Column<int>(type: "integer", nullable: false),
                    BatchSize = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    InitiatedBy = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encryptionkeyrotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_encryptionkeyrotations_encryptionkeys_FromKeyId",
                        column: x => x.FromKeyId,
                        principalTable: "encryptionkeys",
                        principalColumn: "Kid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_encryptionkeyrotations_encryptionkeys_ToKeyId",
                        column: x => x.ToKeyId,
                        principalTable: "encryptionkeys",
                        principalColumn: "Kid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_images_EncryptionKeyId",
                table: "images",
                column: "EncryptionKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_encryptionkeyrotations_FromKeyId",
                table: "encryptionkeyrotations",
                column: "FromKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_encryptionkeyrotations_Status",
                table: "encryptionkeyrotations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_encryptionkeyrotations_ToKeyId",
                table: "encryptionkeyrotations",
                column: "ToKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_encryptionkeys_Status_Version",
                table: "encryptionkeys",
                columns: new[] { "Status", "Version" });

            migrationBuilder.AddForeignKey(
                name: "FK_images_encryptionkeys_EncryptionKeyId",
                table: "images",
                column: "EncryptionKeyId",
                principalTable: "encryptionkeys",
                principalColumn: "Kid",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_images_encryptionkeys_EncryptionKeyId",
                table: "images");

            migrationBuilder.DropTable(
                name: "encryptionkeyrotations");

            migrationBuilder.DropTable(
                name: "encryptionkeys");

            migrationBuilder.DropIndex(
                name: "IX_images_EncryptionKeyId",
                table: "images");

            migrationBuilder.DropColumn(
                name: "AuthenticationTag",
                table: "images");

            migrationBuilder.DropColumn(
                name: "EncryptionKeyId",
                table: "images");

            migrationBuilder.DropColumn(
                name: "InitializationVector",
                table: "images");

            migrationBuilder.DropColumn(
                name: "IsEncrypted",
                table: "images");
        }
    }
}
