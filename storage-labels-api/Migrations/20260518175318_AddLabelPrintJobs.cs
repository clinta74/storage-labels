using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLabelsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddLabelPrintJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "labelprintjobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LabelFormat = table.Column<int>(type: "integer", nullable: false),
                    IncrementAlgorithm = table.Column<int>(type: "integer", nullable: false),
                    AlgorithmPrefix = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AlgorithmSuffixLength = table.Column<int>(type: "integer", nullable: false),
                    LastGeneratedIndex = table.Column<long>(type: "bigint", nullable: false),
                    TotalLabelsGenerated = table.Column<int>(type: "integer", nullable: false),
                    CodeColorPattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labelprintjobs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "labelprintjobs");
        }
    }
}
