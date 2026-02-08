# PostgreSQL Full-Text Search Manual Setup

This document provides instructions for manually setting up PostgreSQL full-text search if the automatic migration fails due to insufficient permissions.

## Prerequisites

- PostgreSQL 12 or higher
- Database access with appropriate permissions
- PostgreSQL user with `CREATE EXTENSION` privilege or superuser access

## Why Manual Setup May Be Required

The EF Core migration attempts to automatically create the `pg_trgm` extension with:
```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

This may fail if:
- The database user lacks `CREATE EXTENSION` privilege
- The extension is not available in the PostgreSQL installation
- Database security policies restrict extension creation

## Error Symptoms

If the automatic migration fails, you may see errors like:
```
ERROR: permission denied to create extension "pg_trgm"
ERROR: could not open extension control file
```

## Manual Installation Steps

### Step 1: Verify pg_trgm Availability

Connect to your PostgreSQL database and check if the extension is available:

```sql
-- List available extensions
SELECT * FROM pg_available_extensions WHERE name = 'pg_trgm';
```

If `pg_trgm` is not listed, you may need to install the `postgresql-contrib` package:

**Ubuntu/Debian:**
```bash
sudo apt-get install postgresql-contrib-17
```

**CentOS/RHEL:**
```bash
sudo yum install postgresql17-contrib
```

**macOS (Homebrew):**
```bash
brew install postgresql@17
```

**Windows:**
The contrib modules are typically included in the PostgreSQL installer. Ensure you selected "PostgreSQL Server" and "Command Line Tools" during installation.

### Step 2: Enable pg_trgm Extension

Connect as a superuser (typically `postgres`) or a user with `CREATE EXTENSION` privilege:

```bash
psql -U postgres -d storage_labels
```

Then enable the extension:

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

Verify the extension is installed:

```sql
SELECT * FROM pg_extension WHERE extname = 'pg_trgm';
```

### Step 3: Grant Necessary Permissions

If your application database user (e.g., `storage_labels_user`) needs to query or manage the extension:

```sql
-- Grant usage on the pg_trgm extension schema (usually public)
GRANT USAGE ON SCHEMA public TO storage_labels_user;
```

### Step 4: Run the Migration

After manually enabling the extension, run the EF Core migration:

```bash
cd storage-labels-api
dotnet ef database update
```

The migration will:
1. Skip `CREATE EXTENSION` (already exists)
2. Create `search_vector` columns on `boxes` and `items` tables
3. Create GIN indexes for full-text search

### Step 5: Verify Installation

Check that the full-text search columns and indexes were created:

```sql
-- Check boxes table
\d+ boxes

-- Verify search_vector column exists
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'boxes' AND column_name = 'search_vector';

-- Check indexes
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename IN ('boxes', 'items') AND indexname LIKE '%search%';
```

Expected output should show:
- `search_vector` column of type `tsvector` on both tables
- `idx_boxes_search` and `idx_items_search` GIN indexes

### Step 6: Test Full-Text Search

Test the search functionality:

```sql
-- Test box search
SELECT "BoxId", "Name", "Code", "Description"
FROM boxes
WHERE search_vector @@ to_tsquery('english', 'test')
LIMIT 5;

-- Test item search
SELECT "ItemId", "Name", "Description"
FROM items
WHERE search_vector @@ to_tsquery('english', 'tool')
LIMIT 5;

-- Test with ranking (relevance scoring)
SELECT "Name", "Code", 
       ts_rank(search_vector, to_tsquery('english', 'electronics')) as rank
FROM boxes
WHERE search_vector @@ to_tsquery('english', 'electronics')
ORDER BY rank DESC
LIMIT 10;
```

## Troubleshooting

### Extension Installation Fails

**Problem:** `could not open extension control file`

**Solution:** Install the postgresql-contrib package for your PostgreSQL version (see Step 1).

### Permission Denied

**Problem:** `ERROR: permission denied to create extension "pg_trgm"`

**Solution:** Contact your database administrator or use a superuser account to enable the extension.

### Migration Already Applied

**Problem:** Migration shows as already applied but extension is missing

**Solution:** Manually run the SQL from the migration file:

```bash
# Connect to database
psql -U postgres -d storage_labels

# Run the migration SQL manually
\i storage-labels-api/Migrations/20260208000149_AddFullTextSearch.sql
```

Or execute the SQL directly:

```sql
-- Enable extension
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Add search_vector to boxes
ALTER TABLE boxes 
ADD COLUMN IF NOT EXISTS search_vector tsvector 
GENERATED ALWAYS AS (
    setweight(to_tsvector('english', coalesce("Name", '')), 'A') ||
    setweight(to_tsvector('english', coalesce("Code", '')), 'A') ||
    setweight(to_tsvector('english', coalesce("Description", '')), 'B')
) STORED;

CREATE INDEX IF NOT EXISTS idx_boxes_search ON boxes USING GIN (search_vector);

-- Add search_vector to items
ALTER TABLE items 
ADD COLUMN IF NOT EXISTS search_vector tsvector 
GENERATED ALWAYS AS (
    setweight(to_tsvector('english', coalesce("Name", '')), 'A') ||
    setweight(to_tsvector('english', coalesce("Description", '')), 'B')
) STORED;

CREATE INDEX IF NOT EXISTS idx_items_search ON items USING GIN (search_vector);
```

### Index Not Being Used

**Problem:** Queries are slow despite having indexes

**Solution:** 

1. Analyze tables to update statistics:
```sql
ANALYZE boxes;
ANALYZE items;
```

2. Verify index is being used with EXPLAIN:
```sql
EXPLAIN ANALYZE
SELECT * FROM boxes
WHERE search_vector @@ to_tsquery('english', 'test');
```

Look for "Bitmap Index Scan" on `idx_boxes_search` in the query plan.

3. If index is not used, ensure you're using the correct query format:
   - ✅ Correct: `WHERE search_vector @@ to_tsquery('english', 'term')`
   - ❌ Wrong: `WHERE to_tsvector('english', "Name") @@ to_tsquery('english', 'term')`

### Docker Container Permissions

**Problem:** Running in Docker with limited user privileges

**Solution:**

1. Create an initialization script for the container:

**docker-entrypoint-initdb.d/01-extensions.sql:**
```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

2. Mount this in your `docker-compose.yml`:
```yaml
services:
  postgres:
    image: postgres:17
    volumes:
      - ./docker-entrypoint-initdb.d:/docker-entrypoint-initdb.d
```

The script runs automatically during container initialization with superuser privileges.

## Performance Tuning

### Adjust GIN Index Parameters

For better performance on large datasets:

```sql
-- Rebuild index with custom parameters
DROP INDEX idx_boxes_search;
CREATE INDEX idx_boxes_search ON boxes USING GIN (search_vector)
WITH (fastupdate = off, gin_pending_list_limit = 4096);

ANALYZE boxes;
```

### Monitor Index Usage

```sql
-- Check index size
SELECT pg_size_pretty(pg_relation_size('idx_boxes_search')) as index_size;

-- Check index usage statistics
SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read, idx_tup_fetch
FROM pg_stat_user_indexes
WHERE indexname LIKE '%search%';
```

### Configure Search Settings

Adjust text search configuration in PostgreSQL:

```sql
-- Create custom text search configuration (optional)
CREATE TEXT SEARCH CONFIGURATION storage_labels (COPY = english);

-- Add custom dictionary for technical terms
-- ALTER TEXT SEARCH CONFIGURATION storage_labels
-- ADD MAPPING FOR asciiword WITH custom_dict, english_stem;
```

## Docker Compose Configuration

For automatic extension setup in Docker environments:

```yaml
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_DB: storage_labels
      POSTGRES_USER: storage_labels_user
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init-db.sql:/docker-entrypoint-initdb.d/01-init.sql
    command: postgres -c shared_preload_libraries=pg_trgm
```

**init-db.sql:**
```sql
-- Auto-enable extension on database creation
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

## Production Deployment Checklist

- [ ] Verify `pg_trgm` extension is enabled
- [ ] Confirm `search_vector` columns exist on both tables
- [ ] Verify GIN indexes are created (`idx_boxes_search`, `idx_items_search`)
- [ ] Run `ANALYZE` on tables after large data loads
- [ ] Monitor index usage with `pg_stat_user_indexes`
- [ ] Set up regular `VACUUM ANALYZE` jobs for index maintenance
- [ ] Document extension dependencies in deployment procedures

## Additional Resources

- [PostgreSQL Full Text Search Documentation](https://www.postgresql.org/docs/current/textsearch.html)
- [pg_trgm Extension Documentation](https://www.postgresql.org/docs/current/pgtrgm.html)
- [GIN Index Documentation](https://www.postgresql.org/docs/current/gin.html)
- [EF Core PostgreSQL Provider](https://www.npgsql.org/efcore/index.html)

## Support

If you continue to experience issues:
1. Check PostgreSQL server logs for detailed error messages
2. Verify PostgreSQL version is 12 or higher: `SELECT version();`
3. Contact your database administrator for assistance with permissions
4. Open an issue on the Storage Labels GitHub repository with error details
