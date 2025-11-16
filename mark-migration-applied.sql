-- Run this against your PostgreSQL database to mark the InitialPostgreSQL migration as applied
-- This tells EF Core that the schema already exists

-- Create the migrations history table if it doesn't exist
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Insert the migration record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251116023950_InitialPostgreSQL', '9.0.0')
ON CONFLICT DO NOTHING;
