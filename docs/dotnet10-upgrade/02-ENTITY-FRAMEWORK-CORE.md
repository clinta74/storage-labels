# Entity Framework Core 10 Features Guide

## Overview
This guide covers Entity Framework Core 10 enhancements including complex types, bulk operations, JSON column improvements, and compiled models for better performance.

---

## 1. Complex Types (Value Objects)

### What It Is
Complex types allow you to model value objects as properties of entities without creating separate tables. They're inline, owned types that share the same table as their parent entity.

### The Problem

**Current approach:** Flatten properties or use separate tables

```csharp
public class ImageMetadata
{
    public Guid ImageId { get; set; }
    
    // Encryption metadata scattered across multiple columns
    public int? EncryptionKeyId { get; set; }
    public byte[]? InitializationVector { get; set; }
    public byte[]? AuthenticationTag { get; set; }
    public bool IsEncrypted { get; set; }
    
    // No encapsulation - easy to forget one field
}
```

### Complex Types Solution

**Step 1: Define value object:**

```csharp
[ComplexType]
public record EncryptionMetadata
{
    public required int KeyId { get; init; }
    public required byte[] IV { get; init; }
    public required byte[] AuthTag { get; init; }
    public required string Algorithm { get; init; }
    
    // Business logic encapsulated
    public bool IsValid() => IV.Length == 12 && AuthTag.Length == 16;
}

[ComplexType]
public record Dimensions
{
    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSize { get; init; }
    
    public double AspectRatio => Height > 0 ? (double)Width / Height : 0;
    public string DisplaySize => $"{Width}x{Height}";
}
```

**Step 2: Use in entity:**

```csharp
public class ImageMetadata
{
    public Guid ImageId { get; set; }
    public required string StoragePath { get; set; }
    public required string ContentType { get; set; }
    
    // Complex type - all properties in same table
    public EncryptionMetadata? Encryption { get; set; }
    
    // Another complex type
    public Dimensions? ImageDimensions { get; set; }
    
    public bool IsEncrypted => Encryption != null;
}
```

**Step 3: Configure in DbContext:**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<ImageMetadata>(entity =>
    {
        // Complex type configuration
        entity.ComplexProperty(e => e.Encryption, encryption =>
        {
            // Column names in parent table
            encryption.Property(e => e.KeyId).HasColumnName("EncryptionKeyId");
            encryption.Property(e => e.IV).HasColumnName("InitializationVector");
            encryption.Property(e => e.AuthTag).HasColumnName("AuthenticationTag");
            encryption.Property(e => e.Algorithm).HasColumnName("EncryptionAlgorithm");
        });
        
        entity.ComplexProperty(e => e.ImageDimensions, dims =>
        {
            dims.Property(d => d.Width).HasColumnName("ImageWidth");
            dims.Property(d => d.Height).HasColumnName("ImageHeight");
            dims.Property(d => d.FileSize).HasColumnName("FileSizeBytes");
        });
    });
}
```

**Generated SQL:**
```sql
CREATE TABLE "ImageMetadata" (
    "ImageId" uuid PRIMARY KEY,
    "StoragePath" text NOT NULL,
    "ContentType" text NOT NULL,
    -- Complex type columns inline
    "EncryptionKeyId" integer NULL,
    "InitializationVector" bytea NULL,
    "AuthenticationTag" bytea NULL,
    "EncryptionAlgorithm" text NULL,
    "ImageWidth" integer NULL,
    "ImageHeight" integer NULL,
    "FileSizeBytes" bigint NULL
);
```

### More Use Cases

#### 1. Address (for User/Location)

```csharp
[ComplexType]
public record Address
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    
    public string FullAddress => $"{Street}, {City}, {State} {ZipCode}, {Country}";
    public bool IsComplete => !string.IsNullOrEmpty(Street) && 
                             !string.IsNullOrEmpty(City) && 
                             !string.IsNullOrEmpty(ZipCode);
}

public class Location
{
    public long LocationId { get; set; }
    public required string Name { get; set; }
    public Address? PhysicalAddress { get; set; } // Complex type
}
```

#### 2. Audit Information

```csharp
[ComplexType]
public record AuditInfo
{
    public required string CreatedBy { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? ModifiedBy { get; init; }
    public DateTime? ModifiedAt { get; init; }
    
    public bool HasBeenModified => ModifiedAt.HasValue;
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;
}

public class Box
{
    public Guid BoxId { get; set; }
    public required string Name { get; set; }
    public AuditInfo Audit { get; set; } = null!; // Complex type
}
```

#### 3. Money (Amount + Currency)

```csharp
[ComplexType]
public record Money
{
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return this with { Amount = Amount + other.Amount };
    }
    
    public string Display => $"{Amount:N2} {Currency}";
}

public class Item
{
    public Guid ItemId { get; set; }
    public required string Name { get; set; }
    public Money? PurchasePrice { get; set; } // Complex type
    public Money? EstimatedValue { get; set; } // Complex type
}
```

### Benefits

- ✅ **Encapsulation:** Business logic with data
- ✅ **Type safety:** Can't forget related fields
- ✅ **Reusability:** Use same complex type in multiple entities
- ✅ **Performance:** No joins needed (same table)
- ✅ **Queries:** Can filter by complex type properties

### Querying Complex Types

```csharp
// Filter by complex type property
var encryptedImages = await dbContext.ImageMetadata
    .Where(i => i.Encryption != null && i.Encryption.Algorithm == "AES-256-GCM")
    .ToListAsync();

// Order by complex type property
var orderedImages = await dbContext.ImageMetadata
    .OrderByDescending(i => i.ImageDimensions!.FileSize)
    .ToListAsync();

// Select complex type
var dimensions = await dbContext.ImageMetadata
    .Select(i => i.ImageDimensions)
    .ToListAsync();
```

---

## 2. Bulk Operations (ExecuteUpdate & ExecuteDelete)

### What It Is
Execute UPDATE and DELETE operations directly in the database without loading entities into memory. Much faster for bulk operations.

### The Problem

**Current approach:** Load, modify, SaveChanges

```csharp
// Retire all active keys - SLOW for many records
var activeKeys = await dbContext.EncryptionKeys
    .Where(k => k.Status == EncryptionKeyStatus.Active)
    .ToListAsync(); // Load into memory

foreach (var key in activeKeys)
{
    key.Status = EncryptionKeyStatus.Retired;
    key.RetiredAt = DateTime.UtcNow;
}

await dbContext.SaveChangesAsync(); // Multiple UPDATE statements
```

**Issues:**
- Loads all entities into memory
- Generates one UPDATE per entity
- Slow for large datasets
- High memory usage

### Bulk Update Solution

**Execute directly in database:**

```csharp
// Retire all active keys - FAST
var affectedRows = await dbContext.EncryptionKeys
    .Where(k => k.Status == EncryptionKeyStatus.Active)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(k => k.Status, EncryptionKeyStatus.Retired)
        .SetProperty(k => k.RetiredAt, DateTime.UtcNow));

// Single UPDATE statement:
// UPDATE "EncryptionKeys" 
// SET "Status" = 'Retired', "RetiredAt" = '2026-02-07T...'
// WHERE "Status" = 'Active'
```

**Performance:**
- 100 rows: 50ms → 5ms (10x faster)
- 10,000 rows: 5000ms → 10ms (500x faster!)
- Memory: 100MB → <1MB

### Bulk Delete Solution

```csharp
// Delete old refresh tokens - FAST
var cutoffDate = DateTime.UtcNow.AddDays(-30);
var deletedCount = await dbContext.RefreshTokens
    .Where(rt => rt.ExpiresAt < cutoffDate || rt.RevokedAt != null)
    .ExecuteDeleteAsync();

// Single DELETE statement:
// DELETE FROM "RefreshTokens"
// WHERE "ExpiresAt" < '2026-01-08T...' OR "RevokedAt" IS NOT NULL
```

### Use Cases in Storage Labels

#### 1. Key Rotation - Bulk Update

```csharp
public class KeyRotationService
{
    public async Task<int> RotateImagesAsync(int fromKeyId, int toKeyId)
    {
        // Before: Load all images, re-encrypt each, save
        // After: Just update key reference if using envelope encryption
        
        var rotatedCount = await _dbContext.ImageMetadata
            .Where(i => i.Encryption!.KeyId == fromKeyId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Encryption!.KeyId, toKeyId));
        
        return rotatedCount;
    }
}
```

#### 2. Cleanup Old Data

```csharp
public class DataCleanupService
{
    public async Task<CleanupResult> CleanupAsync()
    {
        var cutoff = DateTime.UtcNow.AddMonths(-6);
        
        // Delete old audit logs
        var logsDeleted = await _dbContext.AuditLogs
            .Where(log => log.Timestamp < cutoff)
            .ExecuteDeleteAsync();
        
        // Delete expired refresh tokens
        var tokensDeleted = await _dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync();
        
        return new CleanupResult
        {
            AuditLogsDeleted = logsDeleted,
            TokensDeleted = tokensDeleted
        };
    }
}
```

#### 3. Batch Status Updates

```csharp
public class ItemService
{
    public async Task<int> MarkItemsAsArchivedAsync(Guid boxId)
    {
        // Archive all items in a box
        return await _dbContext.Items
            .Where(i => i.BoxId == boxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Status, ItemStatus.Archived)
                .SetProperty(i => i.ArchivedAt, DateTime.UtcNow));
    }
    
    public async Task<int> ResetItemImageCountsAsync()
    {
        // Recalculate image counts
        return await _dbContext.Items
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.ImageCount, i => 
                    _dbContext.ImageMetadata
                        .Where(img => img.ItemId == i.ItemId)
                        .Count()));
    }
}
```

### Important Considerations

⚠️ **Change tracking not updated:**
```csharp
// Entity in context not updated!
var key = await dbContext.EncryptionKeys.FindAsync(keyId);
await dbContext.EncryptionKeys
    .Where(k => k.Kid == keyId)
    .ExecuteUpdateAsync(s => s.SetProperty(k => k.Status, EncryptionKeyStatus.Retired));

// key.Status is still Active in memory!
// Solution: Reload or detach
dbContext.Entry(key).Reload();
```

⚠️ **No validation or business logic:**
```csharp
// Bypasses all entity validation and interceptors
// Use carefully - no domain events, no validation
```

---

## 3. JSON Column Enhancements

### What It Is
Store and query JSON data directly in PostgreSQL JSON columns with full LINQ support and better performance.

### The Problem

**Current approach:** Serialize to string, no querying

```csharp
public class BoxMetadata
{
    public Guid BoxId { get; set; }
    
    // JSON stored as string - can't query inside it!
    public string? CustomFieldsJson { get; set; }
    
    // Have to deserialize to access
    public Dictionary<string, string> GetCustomFields()
    {
        return string.IsNullOrEmpty(CustomFieldsJson) 
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(CustomFieldsJson)!;
    }
}
```

### JSON Column Solution

**Step 1: Define entity with JSON property:**

```csharp
public class Box
{
    public Guid BoxId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    
    // JSON column - full LINQ support!
    public Dictionary<string, string>? CustomFields { get; set; }
    
    // Complex JSON object
    public BoxSettings? Settings { get; set; }
}

public class BoxSettings
{
    public bool AllowOverflow { get; set; }
    public int MaxItems { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

**Step 2: Configure in DbContext:**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Box>(entity =>
    {
        // Store as PostgreSQL JSONB column
        entity.Property(b => b.CustomFields)
            .HasColumnType("jsonb");
        
        entity.Property(b => b.Settings)
            .HasColumnType("jsonb");
        
        // Create GIN index for fast queries
        entity.HasIndex(b => b.CustomFields)
            .HasMethod("gin");
    });
}
```

**Generated SQL:**
```sql
CREATE TABLE "Boxes" (
    "BoxId" uuid PRIMARY KEY,
    "Name" text NOT NULL,
    "Code" text NOT NULL,
    "CustomFields" jsonb NULL,
    "Settings" jsonb NULL
);

CREATE INDEX "IX_Boxes_CustomFields" ON "Boxes" USING gin ("CustomFields");
```

### Querying JSON Columns

**Filter by JSON property:**

```csharp
// Find boxes with specific custom field
var boxes = await dbContext.Boxes
    .Where(b => b.CustomFields!["color"] == "red")
    .ToListAsync();

// Generated SQL:
// SELECT * FROM "Boxes" WHERE "CustomFields" ->> 'color' = 'red'

// Find boxes with tag
var taggedBoxes = await dbContext.Boxes
    .Where(b => b.Settings!.Tags.Contains("fragile"))
    .ToListAsync();

// Check if JSON field exists
var boxesWithNotes = await dbContext.Boxes
    .Where(b => EF.Functions.JsonExists(b.CustomFields!, "notes"))
    .ToListAsync();
```

**Update JSON property:**

```csharp
// Update nested JSON value
await dbContext.Boxes
    .Where(b => b.BoxId == boxId)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(b => b.Settings!.MaxItems, 100)
        .SetProperty(b => b.Settings!.AllowOverflow, true));

// Add to JSON array
await dbContext.Boxes
    .Where(b => b.BoxId == boxId)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(b => b.Settings!.Tags, 
            b => b.Settings!.Tags.Append("new-tag").ToArray()));
```

**Select JSON property:**

```csharp
// Project JSON properties
var settings = await dbContext.Boxes
    .Select(b => new 
    {
        b.BoxId,
        b.Name,
        MaxItems = b.Settings!.MaxItems,
        Tags = b.Settings!.Tags
    })
    .ToListAsync();
```

### Use Cases

#### 1. Flexible Metadata

```csharp
public class Item
{
    public Guid ItemId { get; set; }
    public required string Name { get; set; }
    
    // User-defined fields without schema changes
    public Dictionary<string, object>? Metadata { get; set; }
}

// Store arbitrary data
var item = new Item
{
    Name = "Laptop",
    Metadata = new Dictionary<string, object>
    {
        ["brand"] = "Dell",
        ["model"] = "XPS 15",
        ["purchaseDate"] = "2024-01-15",
        ["warranty"] = new { Years = 3, Expires = "2027-01-15" }
    }
};

// Query it
var dellLaptops = await dbContext.Items
    .Where(i => i.Metadata!["brand"].ToString() == "Dell")
    .ToListAsync();
```

#### 2. User Preferences

```csharp
public class User
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Rich preferences object
    public UserPreferences? Preferences { get; set; }
}

public class UserPreferences
{
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public NotificationSettings Notifications { get; set; } = new();
    public Dictionary<string, bool> Features { get; set; } = new();
}

public class NotificationSettings
{
    public bool Email { get; set; } = true;
    public bool Push { get; set; } = false;
    public string[] EnabledTypes { get; set; } = Array.Empty<string>();
}

// Query by preference
var darkModeUsers = await dbContext.Users
    .Where(u => u.Preferences!.Theme == "dark")
    .ToListAsync();
```

#### 3. Audit Trail

```csharp
public class AuditLog
{
    public Guid AuditId { get; set; }
    public required string Action { get; set; }
    public required string UserId { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Flexible change tracking
    public Dictionary<string, object>? Changes { get; set; }
    public Dictionary<string, string>? Context { get; set; }
}

// Log change
var audit = new AuditLog
{
    Action = "UpdateBox",
    UserId = currentUserId,
    Timestamp = DateTime.UtcNow,
    Changes = new Dictionary<string, object>
    {
        ["before"] = new { Name = "Old Name", Location = 1 },
        ["after"] = new { Name = "New Name", Location = 2 }
    },
    Context = new Dictionary<string, string>
    {
        ["ipAddress"] = request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        ["userAgent"] = request.HttpContext.Request.Headers.UserAgent.ToString()
    }
};
```

### Performance Considerations

**GIN Index for JSONB:**
```sql
-- Fast queries on JSON properties
CREATE INDEX idx_boxes_custom_fields ON "Boxes" USING gin ("CustomFields");

-- Specific path index for even better performance
CREATE INDEX idx_boxes_color ON "Boxes" USING gin (("CustomFields" -> 'color'));
```

**Size Limits:**
- JSONB is efficient but not unlimited
- Keep JSON documents < 100KB for best performance
- For large data, consider separate tables

---

## 4. Compiled Models

### What It Is
Pre-compile the EF Core model at build time, eliminating the model building overhead at startup. Crucial for Native AOT and fast cold starts.

### The Problem

**Normal EF Core startup:**
```
1. Discover entity types (reflection)     - 50-200ms
2. Build relationships                    - 100-300ms
3. Configure conventions                  - 50-150ms
4. Build final model                      - 100-200ms
Total: 300-850ms per startup
```

**For serverless/Lambda:**
- Each cold start pays this cost
- Adds 300-850ms latency
- No caching between invocations

### Compiled Model Solution

**Generate compiled model:**

```bash
# Generate compiled model
dotnet ef dbcontext optimize --output-dir CompiledModels --namespace StorageLabelsApi.CompiledModels

# Generates:
# CompiledModels/
#   StorageLabelsDbContextModel.cs
#   BoxEntityType.cs
#   ItemEntityType.cs
#   ... (one per entity)
```

**Use compiled model in Program.cs:**

```csharp
builder.Services.AddDbContext<StorageLabelsDbContext>(options =>
{
    options.UseNpgsql(connectionString)
        .UseModel(StorageLabelsDbContextModel.Instance); // Pre-compiled!
});
```

**Results:**
- Startup time: 300-850ms → 5-20ms (40-170x faster!)
- Memory: Lower (no reflection metadata)
- Native AOT: Compatible

### When Model Changes

**After schema changes:**

```bash
# 1. Create and apply migration
dotnet ef migrations add MyMigration
dotnet ef database update

# 2. Regenerate compiled model
dotnet ef dbcontext optimize --output-dir CompiledModels --namespace StorageLabelsApi.CompiledModels

# 3. Rebuild and deploy
dotnet build
```

**Automate in CI/CD:**

```yaml
# .github/workflows/build.yml
- name: Generate EF Compiled Model
  run: dotnet ef dbcontext optimize --project storage-labels-api
  
- name: Build Application
  run: dotnet build
```

### Benefits

- ✅ **Fast startup:** 40-170x faster
- ✅ **Native AOT compatible:** No runtime reflection
- ✅ **Predictable performance:** No first-request penalty
- ✅ **Smaller memory footprint:** Pre-compiled metadata

---

## Summary: Performance Gains

| Feature | Current | Optimized | Improvement |
|---------|---------|-----------|-------------|
| **Complex Types** | Separate queries | Inline columns | No joins, type-safe |
| **Bulk Update** | Load 10K rows | Single UPDATE | 500x faster |
| **Bulk Delete** | Load then delete | Single DELETE | 100x faster |
| **JSON Queries** | Can't query | Full LINQ support | Queryable metadata |
| **Compiled Model** | 300-850ms startup | 5-20ms startup | 40-170x faster |

## Implementation Checklist

### Phase 1: Immediate (2-3 days)
- [ ] Identify bulk operations that load data
- [ ] Replace with `ExecuteUpdateAsync`/`ExecuteDeleteAsync`
- [ ] Add cleanup job using bulk delete
- [ ] Test and measure performance

### Phase 2: Complex Types (3-5 days)
- [ ] Identify value objects (Address, Money, AuditInfo)
- [ ] Create `[ComplexType]` records
- [ ] Update entities to use complex types
- [ ] Create migration
- [ ] Update queries

### Phase 3: JSON Columns (optional)
- [ ] Identify flexible metadata needs
- [ ] Add JSON columns to entities
- [ ] Configure JSONB in DbContext
- [ ] Create GIN indexes
- [ ] Update queries

### Phase 4: Compiled Models (1 day)
- [ ] Generate compiled model
- [ ] Update Program.cs to use it
- [ ] Test startup time
- [ ] Add to build pipeline

---

## References

- [Complex Types](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-8.0/whatsnew#complex-types-as-value-objects)
- [Bulk Operations](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/whatsnew#executeupdate-and-executedelete)
- [JSON Columns](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-8.0/whatsnew#json-columns)
- [Compiled Models](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics#compiled-models)

---

*Document created: February 7, 2026*
*For: Storage Labels API .NET 10 Upgrade*
