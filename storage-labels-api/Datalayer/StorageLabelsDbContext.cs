using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Datalayer;

public class StorageLabelsDbContext([NotNull] DbContextOptions options) : DbContext(options)
{
    public DbSet<Box> Boxes { get; set; } = null!;
    public DbSet<CommonLocation> CommonLocations { get; set; } = null!;
    public DbSet<Item> Items { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserLocation> UserLocations { get; set; } = null!;
    public DbSet<ImageMetadata> Images { get; set; } = null!;
    public DbSet<EncryptionKey> EncryptionKeys { get; set; } = null!;
    public DbSet<EncryptionKeyRotation> EncryptionKeyRotations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure table names to use lowercase (PostgreSQL convention)
        modelBuilder.Entity<Box>().ToTable("boxes");
        modelBuilder.Entity<CommonLocation>().ToTable("commonlocations");
        modelBuilder.Entity<Item>().ToTable("items");
        modelBuilder.Entity<Location>().ToTable("locations");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<UserLocation>().ToTable("userlocations");
        modelBuilder.Entity<ImageMetadata>().ToTable("images");
        modelBuilder.Entity<EncryptionKey>().ToTable("encryptionkeys");
        modelBuilder.Entity<CommonLocation>().ToTable("commonlocations");
        modelBuilder.Entity<Item>().ToTable("items");
        modelBuilder.Entity<Location>().ToTable("locations");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<UserLocation>().ToTable("userlocations");
        modelBuilder.Entity<ImageMetadata>().ToTable("images");

        modelBuilder.Entity<Box>()
            .HasKey(box => box.BoxId);

        modelBuilder.Entity<Box>()
            .Property(box => box.ImageMetadataId)
            .HasColumnName("ImageMetadataId");
        
        modelBuilder.Entity<Box>()
            .HasIndex(box => new { box.LocationId, box.Code })
            .IsUnique();

        modelBuilder.Entity<Box>()
            .HasIndex(box => box.Code);

        modelBuilder.Entity<Box>()
            .HasMany(box => box.Items)
            .WithOne(item => item.Box)
            .HasForeignKey(item => item.BoxId);

        modelBuilder.Entity<CommonLocation>()
            .HasKey(commonLocation => commonLocation.CommonLocationId);

        modelBuilder.Entity<Item>()
            .HasKey(item => item.ItemId);

        modelBuilder.Entity<Item>()
            .Property(item => item.ImageMetadataId)
            .HasColumnName("ImageMetadataId");

        modelBuilder.Entity<Location>()
            .HasKey(location => location.LocationId);

        modelBuilder.Entity<Location>()
            .HasMany(location => location.Boxes)
            .WithOne(box => box.Location)
            .HasForeignKey(box => box.LocationId);

        modelBuilder.Entity<Location>()
            .HasMany(location => location.UserLocations)
            .WithOne(userLocation => userLocation.Location)
            .HasForeignKey(userLocation => userLocation.LocationId);

        modelBuilder.Entity<User>()
            .HasKey(user => user.UserId);

        modelBuilder.Entity<User>()
            .HasMany(user => user.UserLocations)
            .WithOne(userLocation => userLocation.User)
            .HasForeignKey(userLocation => userLocation.UserId);

        modelBuilder.Entity<UserLocation>()
            .HasKey(userLocation => new { userLocation.UserId, userLocation.LocationId });

        modelBuilder.Entity<ImageMetadata>()
            .HasKey(img => img.ImageId);

        modelBuilder.Entity<ImageMetadata>()
            .HasMany(img => img.ReferencedByBoxes)
            .WithOne(box => box.ImageMetadata)
            .HasForeignKey(box => box.ImageMetadataId);

        modelBuilder.Entity<ImageMetadata>()
            .HasMany(img => img.ReferencedByItems)
            .WithOne(item => item.ImageMetadata)
            .HasForeignKey(item => item.ImageMetadataId);

        modelBuilder.Entity<EncryptionKey>()
            .HasKey(key => key.Kid);

        modelBuilder.Entity<EncryptionKey>()
            .HasIndex(key => new { key.Status, key.Version });

        modelBuilder.Entity<EncryptionKey>()
            .HasMany(key => key.Images)
            .WithOne(img => img.EncryptionKey)
            .HasForeignKey(img => img.EncryptionKeyId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent key deletion if images exist

        modelBuilder.Entity<ImageMetadata>()
            .HasOne(img => img.EncryptionKey)
            .WithMany(key => key.Images)
            .HasForeignKey(img => img.EncryptionKeyId);

        // Configure EncryptionKeyRotation relationships
        modelBuilder.Entity<EncryptionKeyRotation>()
            .HasOne(r => r.FromKey)
            .WithMany()
            .HasForeignKey(r => r.FromKeyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EncryptionKeyRotation>()
            .HasOne(r => r.ToKey)
            .WithMany()
            .HasForeignKey(r => r.ToKeyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EncryptionKeyRotation>()
            .HasIndex(r => r.Status);
    }
}