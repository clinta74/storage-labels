using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Datalayer;

public class StorageLabelsDbContext([NotNull] DbContextOptions options) : DbContext(options)
{
    public required DbSet<Box> Boxes { get; set; }
    public required DbSet<CommonLocation> CommonLocations { get; set; }
    public required DbSet<Item> Items { get; set; }
    public required DbSet<Location> Locations { get; set; }
    public required DbSet<User> Users { get; set; }
    public required DbSet<UserLocation> UserLocations { get; set; }
    public required DbSet<ImageMetadata> Images { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Box>()
            .HasKey(box => box.BoxId);

        modelBuilder.Entity<Box>()
            .Property(box => box.ImageMetadataId)
            .HasColumnName("ImageMetadataId");
        
        modelBuilder.Entity<Box>()
            .HasIndex(box => new { box.Code, box.BoxId })
            .IsUnique();

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
    }
}