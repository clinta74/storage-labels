using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorageLabelsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTrigramExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pg_trgm extension for trigram similarity search
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // Add GIN trigram indexes for fast substring matching
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_boxes_name_trgm ON boxes USING gin(\"Name\" gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_boxes_code_trgm ON boxes USING gin(\"Code\" gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_boxes_description_trgm ON boxes USING gin(\"Description\" gin_trgm_ops);");
            
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_items_name_trgm ON items USING gin(\"Name\" gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_items_description_trgm ON items USING gin(\"Description\" gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop trigram indexes
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_items_description_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_items_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_boxes_description_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_boxes_code_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_boxes_name_trgm;");
            
            // Note: Not dropping extension in case other features depend on it
            // migrationBuilder.Sql("DROP EXTENSION IF EXISTS pg_trgm;");
        }
    }
}
