using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Tests.TestInfrastructure;

/// <summary>
/// Test-specific DbContext that configures EF Core InMemory provider to ignore PostgreSQL-specific features.
/// This allows unit tests to run without a PostgreSQL database.
/// </summary>
public class TestStorageLabelsDbContext([NotNull] DbContextOptions options) : StorageLabelsDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base configuration for all standard mappings
        base.OnModelCreating(modelBuilder);

        // No PostgreSQL-specific properties to ignore at this time
    }
}
