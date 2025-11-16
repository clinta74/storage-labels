using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLabelsApi.Migrations
{
    /// <inheritdoc />
    public partial class MakeFromKeyIdNullableInRotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "FromKeyId",
                table: "encryptionkeyrotations",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "FromKeyId",
                table: "encryptionkeyrotations",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
