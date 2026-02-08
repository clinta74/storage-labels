using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Handlers.Search;
using StorageLabelsApi.Services;
using StorageLabelsApi.Tests.TestInfrastructure;

namespace StorageLabelsApi.Tests.Handlers.Search;

public class SearchBoxesAndItemsHandlerTests
{
    private readonly TestStorageLabelsDbContext _context;
    private readonly Mock<ILogger<SearchBoxesAndItemsHandler>> _handlerLoggerMock;
    private readonly Mock<ILogger<InMemorySearchService>> _serviceLoggerMock;
    private readonly SearchBoxesAndItemsHandler _handler;
    private readonly string _testUserId = "test-user-123";
    private readonly List<Box> _boxes;
    private readonly List<Item> _items;
    private readonly List<Location> _locations;
    private readonly List<UserLocation> _userLocations;

    public SearchBoxesAndItemsHandlerTests()
    {
        var now = DateTimeOffset.UtcNow;
        
        // Create test data collections
        _locations = new List<Location>
        {
            new Location(
                LocationId: 1,
                Name: "Test Location",
                Created: now,
                Updated: now
            )
        };

        _userLocations = new List<UserLocation>
        {
            new UserLocation(
                UserId: _testUserId,
                LocationId: 1,
                AccessLevel: AccessLevels.Edit,
                Created: now,
                Updated: now
            )
        };

        var box1Id = Guid.NewGuid();
        var box2Id = Guid.NewGuid();
        var box3Id = Guid.NewGuid();

        _boxes = new List<Box>
        {
            new Box(
                BoxId: box1Id,
                Code: "ELEC001",
                Name: "Electronics Box",
                Description: "Storage for electronic components",
                ImageUrl: null,
                ImageMetadataId: null,
                LocationId: 1,
                Created: now,
                Updated: now,
                LastAccessed: now
            ),
            new Box(
                BoxId: box2Id,
                Code: "TOOL001",
                Name: "Tools Storage",
                Description: "Hand tools and power tools",
                ImageUrl: null,
                ImageMetadataId: null,
                LocationId: 1,
                Created: now,
                Updated: now,
                LastAccessed: now
            ),
            new Box(
                BoxId: box3Id,
                Code: "KITCH001",
                Name: "Kitchen Items",
                Description: "Pots, pans, and utensils",
                ImageUrl: null,
                ImageMetadataId: null,
                LocationId: 1,
                Created: now,
                Updated: now,
                LastAccessed: now
            )
        };

        _items = new List<Item>
        {
            new Item(
                ItemId: Guid.NewGuid(),
                BoxId: box1Id,
                Name: "Arduino Uno",
                Description: "Microcontroller board for electronics projects",
                ImageUrl: null,
                ImageMetadataId: null,
                Created: now,
                Updated: now
            ),
            new Item(
                ItemId: Guid.NewGuid(),
                BoxId: box2Id,
                Name: "Screwdriver Set",
                Description: "Phillips and flathead screwdrivers",
                ImageUrl: null,
                ImageMetadataId: null,
                Created: now,
                Updated: now
            ),
            new Item(
                ItemId: Guid.NewGuid(),
                BoxId: box2Id,
                Name: "Drill",
                Description: "Cordless power drill with battery",
                ImageUrl: null,
                ImageMetadataId: null,
                Created: now,
                Updated: now
            )
        };

        // Use real InMemory database
        var options = new DbContextOptionsBuilder<TestStorageLabelsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestStorageLabelsDbContext(options);
        
        // Seed the database
        _context.Locations.AddRange(_locations);
        _context.UserLocations.AddRange(_userLocations);
        _context.Boxes.AddRange(_boxes);
        _context.Items.AddRange(_items);
        _context.SaveChanges();

        // Use InMemorySearchService for tests
        _serviceLoggerMock = new Mock<ILogger<InMemorySearchService>>();
        var searchService = new InMemorySearchService(_context, _serviceLoggerMock.Object);

        _handlerLoggerMock = new Mock<ILogger<SearchBoxesAndItemsHandler>>();
        _handler = new SearchBoxesAndItemsHandler(searchService, _handlerLoggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ReturnsBoxesAndItems()
    {
        // Arrange
        var query = new SearchBoxesAndItemsQuery(
            Query: "electronics",
            UserId: _testUserId,
            LocationId: null,
            BoxId: null,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var results = await result.Value.Results.ToListAsync();
        results.ShouldNotBeEmpty();
        
        // Should find the Electronics Box
        var electronicsResults = results.Where(r => 
            r.BoxName?.Contains("Electronics", StringComparison.OrdinalIgnoreCase) == true ||
            r.ItemName?.Contains("Arduino", StringComparison.OrdinalIgnoreCase) == true);
        electronicsResults.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var query = new SearchBoxesAndItemsQuery(
            Query: "tool",
            UserId: _testUserId,
            LocationId: null,
            BoxId: null,
            PageNumber: 1,
            PageSize: 2
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var results = await result.Value.Results.ToListAsync();
        results.Count.ShouldBeLessThanOrEqualTo(2);
        result.Value.TotalResults.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithPaginationMetadata_ReturnsCorrectValues()
    {
        // Arrange - Use a query that matches multiple items
        var query = new SearchBoxesAndItemsQuery(
            Query: "tool",
            UserId: _testUserId,
            LocationId: null,
            BoxId: null,
            PageNumber: 1,
            PageSize: 2
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.TotalResults.ShouldBeGreaterThan(0);
        // Results should not exceed page size
        var results = await result.Value.Results.ToListAsync();
        results.Count.ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_WithBoxFilter_DoesNotReturnBoxResults()
    {
        // Arrange
        var boxId = _boxes.First().BoxId;
        var query = new SearchBoxesAndItemsQuery(
            Query: "tool",
            UserId: _testUserId,
            LocationId: null,
            BoxId: boxId,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - When filtering by box, should only return items from that box, not the box itself
        result.IsSuccess.ShouldBeTrue();
        var results = await result.Value.Results.ToListAsync();
        results.ShouldAllBe(r => r.Type == "item");
    }

    [Fact]
    public async Task Handle_WithBoxFilter_ReturnsOnlyFromBox()
    {
        // Arrange
        var targetBox = _boxes.First(b => b.Name == "Tools Storage");
        var query = new SearchBoxesAndItemsQuery(
            Query: "drill",
            UserId: _testUserId,
            LocationId: null,
            BoxId: targetBox.BoxId,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var results = await result.Value.Results.ToListAsync();
        results.ShouldAllBe(r => r.BoxId == targetBox.BoxId.ToString());
    }

    [Fact]
    public async Task Handle_WithLocationFilter_ReturnsOnlyFromLocation()
    {
        // Arrange
        var query = new SearchBoxesAndItemsQuery(
            Query: "electronics",
            UserId: _testUserId,
            LocationId: 1,
            BoxId: null,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var results = await result.Value.Results.ToListAsync();
        results.ShouldAllBe(r => r.LocationId == "1");
    }

    [Fact]
    public async Task Handle_WithNoMatches_ReturnsEmptyResults()
    {
        // Arrange
        var query = new SearchBoxesAndItemsQuery(
            Query: "nonexistent-item-xyz",
            UserId: _testUserId,
            LocationId: null,
            BoxId: null,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var results = await result.Value.Results.ToListAsync();
        results.ShouldBeEmpty();
        result.Value.TotalResults.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithNoAccess_ReturnsEmptyResults()
    {
        // Arrange - Use a different user ID that has no access
        var query = new SearchBoxesAndItemsQuery(
            Query: "electronics",
            UserId: "unauthorized-user",
            LocationId: null,
            BoxId: null,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var results = await result.Value.Results.ToListAsync();
        results.ShouldBeEmpty("user has no location access");
    }

    [Fact]
    public async Task Handle_WithRanking_OrdersByRelevance()
    {
        // Arrange - Search for "tool" which appears in multiple places
        var query = new SearchBoxesAndItemsQuery(
            Query: "tool",
            UserId: _testUserId,
            LocationId: null,
            BoxId: null,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var results = await result.Value.Results.ToListAsync();
        if (results.Count > 1)
        {
            // Results should be ordered by rank (higher rank first)
            var ranks = results.Select(r => r.Rank).ToList();
            ranks.ShouldBeInOrder(SortDirection.Descending, "results should be ordered by relevance");
        }
    }

    [Fact]
    public async Task Handle_WithSecondPage_ReturnsCorrectMetadata()
    {
        // Arrange - Use "electronics" which should match Arduino item for pagination test
        var query = new SearchBoxesAndItemsQuery(
            Query: "electronics",
            UserId: _testUserId,
            LocationId: null,
            BoxId: null,
            PageNumber: 2,
            PageSize: 1
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Should have results, even if empty on page 2
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalResults.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithCaseInsensitiveSearch_FindsResults()
    {
        // Arrange - Search with different case
        var query = new SearchBoxesAndItemsQuery(
            Query: "ELECTRONICS",
            UserId: _testUserId,
            LocationId: null,
            BoxId: null,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var results = await result.Value.Results.ToListAsync();
        results.ShouldNotBeEmpty("search should be case-insensitive");
    }

    [Fact]
    public async Task Handle_SearchingInDescription_FindsResults()
    {
        // Arrange - Search for text that only appears in descriptions
        var query = new SearchBoxesAndItemsQuery(
            Query: "microcontroller",
            UserId: _testUserId,
            LocationId: null,
            BoxId: null,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var results = await result.Value.Results.ToListAsync();
        results.ShouldNotBeEmpty("should find items by description");
        results.ShouldContain(r => r.ItemName == "Arduino Uno");
    }

    [Fact]
    public async Task Handle_SearchingInCode_FindsResults()
    {
        // Arrange - Search for box code
        var query = new SearchBoxesAndItemsQuery(
            Query: "ELEC001",
            UserId: _testUserId,
            LocationId: null,
            BoxId: null,
            PageNumber: 1,
            PageSize: 20
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var results = await result.Value.Results.ToListAsync();
        results.ShouldNotBeEmpty("should find boxes by code");
        results.ShouldContain(r => r.BoxCode == "ELEC001");
    }
}
