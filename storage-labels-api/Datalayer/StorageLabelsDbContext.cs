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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Box>()
            .HasIndex(box => new { box.Code, box.BoxId })
            .IsUnique();

        modelBuilder.Entity<Box>()
            .HasMany(box => box.Items)
            .WithOne(item => item.Box)
            .HasForeignKey(item => item.BoxId);

        modelBuilder.Entity<Location>()
            .HasMany(location => location.Boxes)
            .WithOne(box => box.Location)
            .HasForeignKey(box => box.LocationId);

        modelBuilder.Entity<Location>()
            .HasMany(location => location.UserLocations)
            .WithOne(userLocation => userLocation.Location)
            .HasForeignKey(userLocation => userLocation.LocationId);

        modelBuilder.Entity<User>()
            .HasMany(user => user.UserLocations)
            .WithOne(userLocation => userLocation.User)
            .HasForeignKey(userLocation => userLocation.UserId);

        modelBuilder.Entity<UserLocation>()
            .HasKey(userLocation => new { userLocation.UserId, userLocation.LocationId });
    }
}