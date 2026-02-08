# Contributing to Storage Labels

Thank you for your interest in contributing to Storage Labels! This document provides guidelines and patterns to maintain consistency and quality across the codebase.

---

## 🤖 Guidelines for AI Assistants

**If you are an AI coding assistant (GitHub Copilot, Claude, Cursor, etc.), please read this section carefully before generating or modifying code.**

### Critical Patterns to Follow

1. **DTOs MUST use constructor mapping pattern** (see [DTOs with Records](#3-dtos-with-records-and-constructor-mapping))
   - ✅ DO: `public record ItemResponse(...) { public ItemResponse(ItemModel item) : this(...) { } }`
   - ❌ DON'T: Use static `FromEntity()` methods or manual property-by-property mapping

2. **All handlers MUST use MediatR pattern** (see [MediatR for CQRS](#1-mediator-for-cqrs))
   - ✅ DO: `public record CreateItem(...) : IRequest<Result<ItemResponse>>`
   - ❌ DON'T: Create methods directly in endpoints

3. **Validation MUST use FluentValidation** (see [FluentValidation](#2-fluentvalidation-for-input-validation))
   - ✅ DO: Create separate `Validator` classes for each request
   - ❌ DON'T: Validate inline in handlers or endpoints

4. **Logging MUST use LoggerMessage source generators** (see [Structured Logging](#4-structured-logging-with-loggermessage))
   - ✅ DO: `logger.BoxCreated(userId, boxId, locationId);`
   - ❌ DON'T: Use string interpolation like `logger.LogInformation($"Created box {boxId}")`

5. **Route parameters MUST be camelCase with type constraints**
   - ✅ DO: `/{boxId:guid}`, `/{locationId:long}`, `/{kid:int}`
   - ❌ DON'T: Use kebab-case or omit constraints

6. **Endpoint names MUST include version suffixes**
   - ✅ DO: `.WithName("Update Box V2")`
   - ❌ DON'T: Duplicate names across API versions

**Before making changes:**
1. Search the codebase for similar existing patterns
2. Follow the established conventions exactly
3. Review the relevant sections below for detailed guidance
4. Test your changes with `dotnet test`

---

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Environment](#development-environment)
- [Architecture & Patterns](#architecture--patterns)
  - [API (Backend)](#api-backend)
  - [UI (Frontend)](#ui-frontend)
- [Code Style Guidelines](#code-style-guidelines)
- [Testing Requirements](#testing-requirements)
- [Pull Request Process](#pull-request-process)
- [Commit Message Format](#commit-message-format)

---

## Code of Conduct

- Be respectful and constructive
- Focus on the code, not the person
- Welcome newcomers and help them learn
- Follow the established patterns and conventions

---

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/storage-labels.git`
3. Create a feature branch: `git checkout -b feature/your-feature-name`
4. Make your changes following the guidelines below
5. Test thoroughly
6. Submit a pull request

---

## Development Environment

### Prerequisites

**Backend (API):**
- .NET 10.0 SDK
- PostgreSQL 17
- Visual Studio 2022 or VS Code with C# extensions

**Frontend (UI):**
- Node.js 18+ with npm
- Modern browser with developer tools

### Local Tools

This project uses local .NET tools to ensure version consistency across all development environments. Local tools are preferred over global tools to avoid version conflicts.

**To restore local tools:**
```bash
cd storage-labels-api
dotnet tool restore
```

This will install:
- `dotnet-ef` (Entity Framework Core CLI) - version 10.0.2

### Setup

```bash
# Clone repository
git clone https://github.com/clinta74/storage-labels.git
cd storage-labels

# Backend setup
cd storage-labels-api
dotnet tool restore
dotnet restore
dotnet ef database update

# Frontend setup
cd ../storage-labels-ui
npm install

# Run development servers
# Terminal 1 (API):
cd storage-labels-api
dotnet watch run

# Terminal 2 (UI):
cd storage-labels-ui
npm start
```

---

## Architecture & Patterns

### API (Backend)

The API follows **Clean Architecture** principles with a focus on testability, maintainability, and domain-driven design.

#### Required Patterns

##### 1. Mediator Pattern (MediatR)

**All business logic must use the Mediator pattern** for separation of concerns and testability.

**Pattern:**
```csharp
using Mediator;
using Ardalis.Result;

namespace StorageLabelsApi.Handlers.YourFeature;

// Request record
public record YourRequest(string Parameter) : IRequest<Result<YourResponse>>;

// Handler class
public class YourRequestHandler(
    Dependency1 dep1,
    Dependency2 dep2,
    ILogger<YourRequestHandler> logger) 
    : IRequestHandler<YourRequest, Result<YourResponse>>
{
    public async ValueTask<Result<YourResponse>> Handle(
        YourRequest request, 
        CancellationToken cancellationToken)
    {
        // Validate using FluentValidation
        var validation = await new YourRequestValidator()
            .ValidateAsync(request, cancellationToken);
        
        if (!validation.IsValid)
        {
            return Result<YourResponse>.Invalid(validation.AsErrors());
        }

        // Business logic here
        logger.LogInformation("Processing request...");
        
        try
        {
            // ... implementation
            return Result<YourResponse>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process request");
            return Result<YourResponse>.Error("Error message");
        }
    }
}
```

**✅ DO:**
- Use `IRequest<Result<T>>` for all requests
- Return `Result<T>` from Ardalis.Result for consistent error handling
- Use primary constructors for dependency injection
- Use `ValueTask<Result<T>>` for handler return types
- Place handlers in `Handlers/FeatureName/` folder
- Use descriptive record names (e.g., `CreateBox`, `UpdateItem`, `DeleteLocation`)

**❌ DON'T:**
- Put business logic directly in controllers/endpoints
- Use exceptions for expected validation failures
- Mix concerns (keep handlers focused on single responsibility)

---

##### 2. FluentValidation

**All input validation must use FluentValidation** for consistency and clarity.

**Pattern:**
```csharp
using FluentValidation;

public class YourRequestValidator : AbstractValidator<YourRequest>
{
    public YourRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");
    }
}
```

**✅ DO:**
- Create a separate validator class for each request
- Use descriptive error messages
- Use `.When()` for conditional validation
- Validate all user input at the handler level
- Use built-in validators when possible (NotEmpty, EmailAddress, etc.)

**❌ DON'T:**
- Validate in multiple places (only in validators)
- Use magic strings for error messages
- Skip validation for "simple" inputs

---

##### 3. DTOs with Records and Constructor Mapping

**Use records for all DTOs** (Data Transfer Objects) for immutability and conciseness. **Response DTOs should include a constructor that accepts the internal/domain model** for clean, consistent mapping.

**Pattern:**
```csharp
namespace StorageLabelsApi.Models.DTO.YourFeature;

// Request DTOs
public record CreateItemRequest(
    string Name,
    string? Description,
    int BoxId,
    int Quantity = 1
);

// Response DTOs with constructor mapping
public record ItemResponse(
    int Id,
    string Name,
    string? Description,
    int BoxId,
    string BoxCode,
    int Quantity,
    DateTime Created,
    DateTime? Updated
)
{
    // Constructor that accepts the internal/domain model
    public ItemResponse(ItemModel item) : this(
        item.Id,
        item.Name,
        item.Description,
        item.BoxId,
        item.BoxCode,
        item.Quantity,
        item.Created,
        item.Updated
    )
    { }
};

// Usage in endpoints:
var items = result.Value.Select(item => new ItemResponse(item)).ToList();

// Use positional parameters for simple DTOs
// Use property syntax for complex DTOs with many optional fields
public record UpdateItemRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Quantity { get; init; }
}
```

**Architecture:**
```
Models/DTO/FeatureName/     <- DTOs (API boundary only)
    ItemResponse.cs
    CreateItemRequest.cs
    
Models/FeatureName/          <- Internal/domain models (used by services/handlers)
    ItemModel.cs
    SearchResult.cs
```

**✅ DO:**
- Use records for immutability
- Add constructor to response DTOs that accepts internal model
- Keep DTOs in `Models/DTO/` namespace (API boundary only)
- Keep internal models in `Models/` namespace (service/handler layer)
- Use nullable reference types appropriately
- Provide default values where sensible
- Include all necessary data (don't make clients call multiple endpoints)
- Map at the endpoint boundary: `.Select(model => new ResponseDto(model))`

**❌ DON'T:**
- Use classes for DTOs unless mutability is required
- Expose database entities directly in API responses
- Use DTOs in service/handler layers (use internal models instead)
- Use verbose property-by-property mapping when constructor exists
- Include unnecessary fields (e.g., sensitive data, internal IDs)
- Mix DTOs and internal models in the same namespace

---

##### 4. Structured Logging with LoggerMessage

**Use LoggerMessage source generators** for high-performance, structured logging.

**Pattern:**
```csharp
// Create a partial class in Logging/LogMessages.YourFeature.cs
namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        Message = "User {userId} created box {boxId} in location {locationId}",
        Level = LogLevel.Information)]
    public static partial void BoxCreated(
        this ILogger logger, 
        string userId, 
        int boxId, 
        int locationId);

    [LoggerMessage(
        Message = "Failed to delete item {itemId} - {reason}",
        Level = LogLevel.Warning)]
    public static partial void ItemDeleteFailed(
        this ILogger logger, 
        int itemId, 
        string reason);

    [LoggerMessage(
        Message = "Database query for items in box {boxId} took {elapsed}ms",
        Level = LogLevel.Debug)]
    public static partial void QueryPerformance(
        this ILogger logger, 
        int boxId, 
        long elapsed);
}

// Usage in handlers:
logger.BoxCreated(userId, box.Id, box.LocationId);
logger.ItemDeleteFailed(itemId, "Item not found");
```

**✅ DO:**
- Use structured logging with named parameters
- Create one partial class per feature area
- Use appropriate log levels (Debug, Information, Warning, Error)
- Log all significant operations
- Include context (user ID, entity IDs, etc.)

**❌ DON'T:**
- Use string interpolation in log messages
- Log sensitive data (passwords, tokens, PII)
- Over-log (avoid logging every minor step)
- Use generic messages without context

---

##### 5. Endpoint Mapping

**Use Minimal APIs with proper organization** in the `Endpoints/` folder.

**Pattern:**
```csharp
namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapYourFeature(this IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("your-feature")
            .WithTags("YourFeature")
            .RequireAuthorization(); // If auth required

        group.MapGet("/", GetAllItems)
            .WithName("Get All Items")
            .Produces<IEnumerable<ItemResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:int}", GetItemById)
            .WithName("Get Item By ID")
            .Produces<ItemResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateItem)
            .WithName("Create Item")
            .Produces<ItemResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:int}", UpdateItem)
            .WithName("Update Item")
            .Produces<ItemResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", DeleteItem)
            .WithName("Delete Item")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return routeBuilder;
    }

    private static async Task<IResult> GetAllItems(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllItems(), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> CreateItem(
        CreateItemRequest request,
        HttpContext context,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var result = await mediator.Send(
            new CreateItem(userId, request.Name, request.Description, request.BoxId), 
            cancellationToken);
        return result.ToMinimalApiResult();
    }
}
```

**✅ DO:**
- Use route groups for related endpoints
- Use route constraints (`:int`, `:guid`, etc.)
- Document all possible response types with `.Produces<T>()`
- Use `HttpContext.GetUserId()` for authenticated user ID
- Keep endpoint methods thin (just wire to mediator)
- Use meaningful route names for documentation

**❌ DON'T:**
- Put business logic in endpoint methods
- Use ambiguous routes
- Forget to specify response types

---

##### 6. Database Access with Entity Framework Core

**Use EF Core with proper patterns** for data access.

**Pattern:**
```csharp
// Entity (in Datalayer/Models/)
public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BoxId { get; set; }
    public int Quantity { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    
    // Navigation properties
    public Box Box { get; set; } = null!;
}

// DbContext configuration (in Datalayer/StorageLabelsDbContext.cs)
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Item>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Description).HasMaxLength(1000);
        
        entity.HasOne(e => e.Box)
            .WithMany(b => b.Items)
            .HasForeignKey(e => e.BoxId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}

// Usage in handlers
var item = await dbContext.Items
    .Include(i => i.Box)
    .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

if (item == null)
{
    return Result<ItemResponse>.NotFound();
}
```

**✅ DO:**
- Use async methods with CancellationToken
- Use `.Include()` for eager loading when needed
- Use `.AsNoTracking()` for read-only queries
- Configure entities in `OnModelCreating`
- Use migrations for schema changes
- Use `TimeProvider` for timestamps (not `DateTime.Now`)

**❌ DON'T:**
- Use lazy loading (configure explicit loading)
- Return entities directly (map to DTOs)
- Forget to check for null after queries
- Use `DateTime.Now` or `DateTime.UtcNow` (use injected `TimeProvider`)

---

##### 7. Performance Optimizations (.NET 10)

**Apply performance optimizations** for high-traffic scenarios using .NET 10 features.

###### FrozenSet and FrozenDictionary

For read-only collections that are initialized once and queried frequently, use frozen collections for optimal performance.

**Pattern:**
```csharp
using System.Collections.Frozen;

public static class Policies
{
    public const string Write_User = "write:user";
    public const string Read_User = "read:user";
    
    // FrozenSet for O(1) lookups with zero allocations (16x faster than array)
    public static readonly FrozenSet<string> AllPermissions = new[]
    {
        Write_User,
        Read_User,
        // ... more permissions
    }.ToFrozenSet();
    
    // FrozenDictionary for role-to-permissions mapping
    private static readonly FrozenDictionary<string, FrozenSet<string>> RolePermissions =
        new Dictionary<string, FrozenSet<string>>
        {
            ["Admin"] = AllPermissions,
            ["User"] = new[] { Read_User }.ToFrozenSet()
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}

// Usage in authorization
if (Policies.AllPermissions.Contains(requiredPermission)) // O(1) lookup
{
    // Grant access
}
```

**When to use:**
- Permission/role lookups (checked on every request)
- Configuration constants
- File extension/MIME type mappings
- Any read-only data queried frequently (>1000 times/sec)

**Performance gains:**
- 4-16x faster lookups than HashSet
- Zero allocations on repeated queries
- Thread-safe by default (immutable)

###### AsNoTracking for Read-Only Queries

For all GET operations that don't modify entities, use `AsNoTracking()` to improve performance.

**Pattern:**
```csharp
public class GetBoxByIdHandler(StorageLabelsDbContext dbContext) 
    : IRequestHandler<GetBoxById, Result<Box>>
{
    public async ValueTask<Result<Box>> Handle(
        GetBoxById request, 
        CancellationToken cancellationToken)
    {
        var box = await dbContext.Boxes
            .AsNoTracking() // ← Add this for read-only queries
            .Where(b => b.BoxId == request.BoxId)
            .Where(b => b.Location.UserLocations.Any(
                ul => ul.UserId == request.UserId))
            .FirstOrDefaultAsync(cancellationToken);

        if (box is null)
        {
            return Result.NotFound($"Box with id {request.BoxId} was not found.");
        }

        return Result.Success(box);
    }
}
```

**✅ Always use AsNoTracking() for:**
- All GET/read operations
- Search queries
- List/collection retrievals
- Queries that map to DTOs

**❌ Never use AsNoTracking() for:**
- UPDATE operations (need change tracking)
- DELETE operations (need entity tracking)
- Queries where you'll modify entities

**Performance gains:**
- 20-30% faster query execution
- 30-50% reduced memory allocations
- No change tracking overhead

---

##### 8. Trigram Search with PostgreSQL

**Use PostgreSQL trigram search (pg_trgm)** for efficient substring searching across large datasets.

###### Setup

The project uses PostgreSQL's `pg_trgm` extension with GIN trigram indexes for fast substring matching.

**Migration pattern:**
```csharp
public partial class AddFullTextSearch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Enable pg_trgm extension for fuzzy matching
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        
        // Add search vector column (auto-updated with triggers)
        migrationBuilder.Sql(@"
            ALTER TABLE boxes 
            ADD COLUMN search_vector tsvector 
            GENERATED ALWAYS AS (
                setweight(to_tsvector('english', coalesce(""Name"", '')), 'A') ||
                setweight(to_tsvector('english', coalesce(""Code"", '')), 'A') ||
                setweight(to_tsvector('english', coalesce(""Description"", '')), 'B')
            ) STORED;
            
            CREATE INDEX idx_boxes_search ON boxes USING GIN (search_vector);
        ");
    }
}
```

**Handler pattern (v2 search with ranking):**
```csharp
public class SearchBoxesAndItemsV2Handler(
    StorageLabelsDbContext dbContext,
    ILogger<SearchBoxesAndItemsV2Handler> logger) 
    : IRequestHandler<SearchBoxesAndItemsQueryV2, Result<SearchResultsResponseV2>>
{
    public async ValueTask<Result<SearchResultsResponseV2>> Handle(
        SearchBoxesAndItemsQueryV2 request, 
        CancellationToken cancellationToken)
    {
        // Use PlainToTsQuery for natural language search
        var boxQuery = dbContext.Boxes
            .AsNoTracking()
            .Where(b => EF.Functions.ToTsVector("english", 
                b.Name + " " + b.Code + " " + (b.Description ?? ""))
                .Matches(EF.Functions.PlainToTsQuery("english", request.Query)))
            .Select(b => new SearchResultV2
            {
                Type = "box",
                BoxId = b.BoxId.ToString(),
                BoxName = b.Name,
                // ... other fields
                
                // Rank for relevance scoring
                Rank = EF.Functions.ToTsVector("english", 
                    b.Name + " " + b.Code + " " + (b.Description ?? ""))
                    .Rank(EF.Functions.PlainToTsQuery("english", request.Query))
            });

        var results = await boxQuery.ToListAsync(cancellationToken);
        
        // Sort by rank (descending) for best matches first
        return results.OrderByDescending(r => r.Rank).ToList();
    }
}
```

**Key concepts:**
- `ToTsVector("english", text)`: Converts text to searchable tsvector
- `PlainToTsQuery("english", query)`: Converts user query to tsquery (handles spaces as AND)
- `Matches()`: Tests if tsvector matches tsquery
- `Rank()`: Returns relevance score (0.0 to 1.0)
- `setweight()`: Assigns importance to fields (A=highest, D=lowest)

**Performance comparison:**
- **LIKE queries**: 500-2000ms on 100,000 rows
- **Trigram search**: 5-20ms on 100,000 rows with substring matching
- **50-100x faster!**

**✅ DO:**
- Use `AsNoTracking()` on search queries
- Use `PlainToTsQuery` for user-friendly search (auto-handles AND logic)
- Add GIN indexes on tsvector columns
- Use ranking to sort results by relevance
- Add pagination for large result sets

**❌ DON'T:**
- Use multiple `LIKE` queries (very slow)
- Load entire tables into memory for filtering
- Forget to add indexes on search columns
- Return unranked results (users expect relevance sorting)

---

##### 9. API Versioning

**Use explicit versioning** for breaking API changes while maintaining backward compatibility.

###### Versioning Strategy

- **v1 (implicit)**: `/api/endpoint` - Original API, maintained for backward compatibility
- **v2 (explicit)**: `/api/v2/endpoint` - New features with breaking changes
- **Deprecation**: v1 endpoints marked deprecated with 6-month sunset period

**Endpoint mapping pattern:**
```csharp
// MapAll.cs - Register all API endpoints
public static void MapAllEndpoints(this IEndpointRouteBuilder app)
{
    var apiVersionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1, 0))
        .HasApiVersion(new ApiVersion(2, 0))
        .Build();

    // v1 API group (implicit versioning, backward compatible)
    var v1 = app.MapGroup("/api")
        .WithApiVersionSet(apiVersionSet)
        .WithOpenApi();

    v1.MapSearchEndpointsV1();  // Original search with LIKE
    v1.MapBoxEndpoints();       // Boxes (no changes)
    v1.MapItemEndpoints();      // Items (no changes)
    // ... other v1 endpoints

    // v2 API group (explicit versioning, new features)
    var v2 = app.MapGroup("/api/v2")
        .WithApiVersionSet(apiVersionSet)
        .HasApiVersion(2, 0)
        .WithOpenApi();

    v2.MapSearchEndpointsV2();  // New FTS with pagination
    v2.MapBoxEndpoints();       // Same implementation as v1
    v2.MapItemEndpoints();      // Same implementation as v1
    // ... all v2 endpoints
}
```

**Search endpoint example:**
```csharp
// MapSearch.cs
public static class MapSearch
{
    public static void MapSearchEndpointsV1(this IEndpointRouteBuilder app)
    {
        app.MapGet("search", async (
            [FromQuery] string q,
            [FromServices] IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchBoxesAndItemsQuery(q, userId, null, null);
            var result = await mediator.Send(query, cancellationToken);
            return result.ToMinimalApiResult();
        })
        .MapToApiVersion(1, 0)
        .WithSummary("Search boxes and items (v1, deprecated)")
        .WithDescription("Legacy search using LIKE queries. Use v2 for better performance.")
        .WithOpenApi(op => new(op)
        {
            Deprecated = true // Mark as deprecated
        });
    }

    public static void MapSearchEndpointsV2(this IEndpointRouteBuilder app)
    {
        app.MapGet("search", async (
            [FromQuery] string q,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] IMediator mediator,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var query = new SearchBoxesAndItemsQueryV2(
                q, userId, null, null, pageNumber, pageSize);
            var result = await mediator.Send(query, cancellationToken);
            return result.ToMinimalApiResult();
        })
        .MapToApiVersion(2, 0)
        .WithSummary("Search boxes and items with FTS (v2)")
        .WithDescription("Trigram-based search with pagination and relevance ranking")
        .WithOpenApi();
    }
}
```

**DTO versioning pattern:**
```csharp
// V1 Response (original)
public class SearchResultsResponse
{
    public List<SearchResultResponse> Results { get; set; } = [];
}

public class SearchResultResponse
{
    public string Type { get; set; } = string.Empty;
    public string? BoxId { get; set; }
    public string? BoxName { get; set; }
    // ... fields
}

// V2 Response (with pagination and ranking)
public class SearchResultsResponseV2
{
    public List<SearchResultV2> Results { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalResults { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class SearchResultV2 : SearchResultResponse
{
    public double Rank { get; set; } // Relevance score 0.0-1.0
}
```

**Version management:**
1. **Each version should be complete**: Both v1 and v2 should have all endpoints needed for full functionality
2. **Mark deprecated endpoints**: Use `Deprecated = true` in OpenAPI
3. **Document breaking changes**: Add migration guide in API docs
4. **Sunset timeline**: 6 months minimum before removing deprecated endpoints
5. **UI coordination**: Update UI version when adopting new API version

**✅ DO:**
- Provide complete API surface for each version
- Document all breaking changes
- Add pagination to new endpoints
- Include version in response DTOs
- Test both versions work correctly

**❌ DON'T:**
- Make breaking changes without new version
- Remove old version without deprecation period
- Mix v1 and v2 DTOs in single response
- Forget to update UI when adding v2 endpoints

---

### UI (Frontend)

The UI uses **React 18** with **Material-UI v7**, **React Router**, and **Context API** for state management.

#### Required Patterns

##### 1. Component Structure

**Use functional components with TypeScript** for all UI components.

**Pattern:**
```tsx
import React, { useState, useEffect } from 'react';
import { Box, Button, Typography, CircularProgress } from '@mui/material';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';

interface YourComponentProps {
    itemId: number;
    onUpdate?: (item: ItemResponse) => void;
}

export const YourComponent: React.FC<YourComponentProps> = ({ itemId, onUpdate }) => {
    const { Api } = useApi();
    const alert = useAlertMessage();
    const [item, setItem] = useState<ItemResponse | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        loadItem();
    }, [itemId]);

    const loadItem = async () => {
        try {
            setLoading(true);
            const { data } = await Api.Item.getItem(itemId);
            setItem(data);
        } catch (error: any) {
            alert.addError(error.response?.data?.message || 'Failed to load item');
        } finally {
            setLoading(false);
        }
    };

    const handleUpdate = async () => {
        try {
            const { data } = await Api.Item.updateItem(itemId, { name: 'Updated' });
            setItem(data);
            onUpdate?.(data);
        } catch (error: any) {
            alert.addError(error.response?.data?.message || 'Failed to update item');
        }
    };

    if (loading) {
        return <CircularProgress />;
    }

    if (!item) {
        return <Typography>Item not found</Typography>;
    }

    return (
        <Box>
            <Typography variant="h5">{item.name}</Typography>
            <Button onClick={handleUpdate} color="primary">
                Update
            </Button>
        </Box>
    );
};
```

**✅ DO:**
- Use TypeScript for all components
- Define prop interfaces
- Use functional components with hooks
- Handle loading and error states
- Use Material-UI components consistently
- Export components as named exports

**❌ DON'T:**
- Use class components
- Ignore TypeScript errors
- Leave API errors unhandled
- Use inline styles (use sx prop or theme)

---

##### 2. API Integration

**Use axios with typed endpoints** in the `api/` folder.

**Pattern:**
```typescript
// api/endpoints/item.ts
import { AxiosInstance } from 'axios';

export type ItemEndpoints = ReturnType<typeof getItemEndpoints>;

export const getItemEndpoints = (client: AxiosInstance) => ({
    getItems: () =>
        client.get<ItemResponse[]>('item'),

    getItem: (id: number) =>
        client.get<ItemResponse>(`item/${id}`),

    createItem: (request: CreateItemRequest) =>
        client.post<ItemResponse>('item', request),

    updateItem: (id: number, request: UpdateItemRequest) =>
        client.put<ItemResponse>(`item/${id}`, request),

    deleteItem: (id: number) =>
        client.delete(`item/${id}`),
});
```

**✅ DO:**
- Define typed response interfaces
- Use consistent naming (getX, createX, updateX, deleteX)
- Return full axios response (allows access to headers, status, etc.)
- Group endpoints by feature

**❌ DON'T:**
- Make API calls directly from components (use endpoint functions)
- Ignore TypeScript types
- Hard-code URLs (use base URL from config)

---

##### 3. State Management with Context

**Use React Context** for shared state across components.

**Pattern:**
```tsx
// providers/your-provider.tsx
import React, { createContext, PropsWithChildren, useState, useContext } from 'react';

interface YourContextType {
    data: SomeData | null;
    loading: boolean;
    updateData: (data: SomeData) => void;
}

const YourContext = createContext<YourContextType | null>(null);

export const YourProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const [data, setData] = useState<SomeData | null>(null);
    const [loading, setLoading] = useState(false);

    const updateData = (newData: SomeData) => {
        setData(newData);
    };

    return (
        <YourContext.Provider value={{ data, loading, updateData }}>
            {children}
        </YourContext.Provider>
    );
};

export const useYourContext = () => {
    const context = useContext(YourContext);
    if (context === null) {
        throw new Error('useYourContext must be used within YourProvider');
    }
    return context;
};
```

**✅ DO:**
- Create typed context interfaces
- Throw error if context used outside provider
- Use custom hooks for context access
- Keep context focused (single responsibility)

**❌ DON'T:**
- Create massive global contexts
- Use context for frequently changing data (use local state)
- Forget to wrap components in providers

---

##### 4. Form Handling

**Use Material-UI components with controlled inputs**.

**Pattern:**
```tsx
import React, { useState } from 'react';
import { Box, Button, TextField, Stack } from '@mui/material';

interface FormData {
    name: string;
    quantity: number;
}

export const ItemForm: React.FC = () => {
    const [formData, setFormData] = useState<FormData>({
        name: '',
        quantity: 1,
    });
    const [loading, setLoading] = useState(false);

    const handleChange = (field: keyof FormData) => (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        setFormData(prev => ({
            ...prev,
            [field]: e.target.value,
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);
        try {
            // API call
        } finally {
            setLoading(false);
        }
    };

    return (
        <Box component="form" onSubmit={handleSubmit}>
            <TextField
                fullWidth
                label="Name"
                value={formData.name}
                onChange={handleChange('name')}
                margin="normal"
                required
            />

            <TextField
                fullWidth
                label="Quantity"
                type="number"
                value={formData.quantity}
                onChange={handleChange('quantity')}
                margin="normal"
            />

            <Stack direction="row" spacing={2} padding={2} justifyContent="right">
                <Button type="submit" color="primary" loading={loading}>
                    Save
                </Button>
            </Stack>
        </Box>
    );
};
```

**✅ DO:**
- Use controlled inputs (value + onChange)
- Use TypeScript for form data
- Add loading states to buttons
- Use Stack for button layout
- Validate on submit

**❌ DON'T:**
- Use uncontrolled inputs
- Submit forms without preventing default
- Forget disabled/loading states

---

##### 5. Navigation with React Router

**Use declarative navigation** with Link components.

**Pattern:**
```tsx
import { Link, useNavigate } from 'react-router';
import { Button } from '@mui/material';

// Declarative navigation (preferred)
<Button component={Link} to="/items">
    View Items
</Button>

// Programmatic navigation (when needed)
const navigate = useNavigate();

const handleSave = async () => {
    await saveItem();
    navigate('/items'); // After successful operation
};
```

**✅ DO:**
- Use `component={Link} to="/path"` for navigation buttons
- Use `navigate()` after successful operations
- Use relative paths when appropriate

**❌ DON'T:**
- Use `onClick={() => navigate()}` when Link is sufficient
- Navigate before confirming operations
- Use hard-coded URLs

---

##### 6. Error Handling & Notifications

**Use AlertProvider for errors and SnackbarProvider for success messages**.

**Pattern:**
```tsx
import { useAlertMessage } from '../../providers/alert-provider';
import { useSnackbar } from '../../providers/snackbar-provider';

export const YourComponent: React.FC = () => {
    const alert = useAlertMessage();
    const { showSuccess } = useSnackbar();

    const handleDelete = async (id: number) => {
        try {
            await Api.Item.deleteItem(id);
            showSuccess('Item deleted successfully');
        } catch (error: any) {
            const message = error.response?.data?.message || 'Failed to delete item';
            alert.addError(message);
        }
    };
};
```

**✅ DO:**
- Use snackbar for success messages (auto-dismiss)
- Use alert for error messages (persistent)
- Extract error messages from API responses
- Provide user-friendly error messages

**❌ DON'T:**
- Use `console.error` for user-facing errors
- Show raw error objects to users
- Use alert() or confirm() browser dialogs

---

##### 7. Styling with Material-UI

**Use the sx prop and theme** for consistent styling.

**Pattern:**
```tsx
import { Box, Typography } from '@mui/material';

<Box sx={{ p: 3, bgcolor: 'background.paper', borderRadius: 1 }}>
    <Typography variant="h5" sx={{ mb: 2, color: 'primary.main' }}>
        Title
    </Typography>
</Box>

// Use theme spacing (multiples of 8px)
sx={{ mt: 2 }}  // margin-top: 16px
sx={{ p: 3 }}   // padding: 24px
```

**✅ DO:**
- Use sx prop for styling
- Use theme values (colors, spacing)
- Use variant prop for Typography
- Use consistent spacing multiples

**❌ DON'T:**
- Use inline styles
- Hard-code colors or sizes
- Use CSS classes unless necessary

---

## Code Style Guidelines

### TypeScript/JavaScript (UI)

```typescript
// ✅ Good
const getUserName = (user: User): string => {
    return user.firstName + ' ' + user.lastName;
};

// ❌ Bad
function getUserName(user) {
    return user.firstName + ' ' + user.lastName;
}
```

**Rules:**
- Use arrow functions for consistency
- Always include types
- Use `const` instead of `let` when possible
- Use destructuring for props and objects

### C# (API)

```csharp
// ✅ Good
public record CreateBoxRequest(string Code, int LocationId);

public class CreateBoxHandler(StorageLabelsDbContext dbContext)
    : IRequestHandler<CreateBox, Result<BoxResponse>>
{
    // Implementation
}

// ❌ Bad
public class CreateBoxRequest
{
    public string Code { get; set; }
    public int LocationId { get; set; }
}
```

**Rules:**
- Use records for DTOs
- Use primary constructors for dependency injection
- Follow .NET naming conventions (PascalCase)
- Use `var` for local variables when type is obvious

---

## Testing Requirements

### Backend (C#)

**All handlers must have unit tests** using xUnit, FluentAssertions, and Moq.

**Pattern:**
```csharp
public class CreateBoxHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesBox()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var handler = new CreateBoxHandler(dbContext, TimeProvider.System);
        var request = new CreateBox("BOX001", 1);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("BOX001");
    }

    [Fact]
    public async Task Handle_DuplicateCode_ReturnsError()
    {
        // Arrange, Act, Assert
    }
}
```

**Requirements:**
- Minimum 80% code coverage
- Test happy path and error cases
- Use descriptive test names
- Use FluentAssertions for readable assertions

### Frontend (React)

Unit tests are encouraged but not required for all components. Focus on:
- Complex logic components
- Utility functions
- Context providers

---

## Pull Request Process

1. **Create a feature branch** from `main`
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Follow the coding patterns** described above

3. **Write/update tests** as needed

4. **Update documentation** if adding new features

5. **Commit with meaningful messages** (see below)

6. **Push and create PR**
   ```bash
   git push origin feature/your-feature-name
   ```

7. **PR checklist:**
   - [ ] Code follows established patterns
   - [ ] Tests added/updated
   - [ ] Documentation updated
   - [ ] No linting errors
   - [ ] All tests pass
   - [ ] Commits follow convention

---

## Commit Message Format

Use **Conventional Commits** format:

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic change)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Examples

```
feat(items): Add bulk import functionality

Implement CSV import for items with validation and error reporting.
Includes progress tracking and rollback on errors.

Closes #123
```

```
fix(auth): Prevent race condition in UserProvider

Add loading state to UserProvider to prevent concurrent API calls
when token becomes available after login.

Fixes #456
```

```
docs: Update contributing guide with API patterns

Add comprehensive patterns for Mediator, FluentValidation, and
structured logging to help new contributors.
```

---

## Questions?

- Check existing code for examples
- Review the README.md and DOCKER.md
- Ask in discussions or issues

Thank you for contributing to Storage Labels! 🎉
