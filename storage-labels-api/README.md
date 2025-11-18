# Storage Labels API

A .NET 9.0 Web API backend for managing physical storage items with built-in authentication, authorization, and encryption capabilities.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Design Patterns](#design-patterns)
- [Data Flow](#data-flow)
- [Authentication & Authorization](#authentication--authorization)
- [Logging](#logging)
- [Database](#database)
- [Error Handling](#error-handling)
- [Code Conventions](#code-conventions)
- [Adding New Features](#adding-new-features)

## Architecture Overview

The API follows **Clean Architecture** principles with a focus on:

- **Separation of Concerns**: Clear boundaries between HTTP layer, business logic, and data access
- **Mediator Pattern**: Decoupled request/response handling via Mediator
- **Result Pattern**: Explicit error handling with Ardalis.Result
- **Dependency Injection**: Constructor-based DI for testability
- **Logging**: Structured logging with source generators

### Technology Stack

- **.NET 9.0**: Latest LTS framework
- **ASP.NET Core Identity**: User management and authentication
- **Mediator**: Lightweight mediator pattern implementation
- **Ardalis.Result**: Railway-oriented programming for error handling
- **Entity Framework Core**: ORM for database operations
- **PostgreSQL**: Primary database
- **FluentValidation**: Request validation
- **JWT Bearer**: Token-based authentication

## Project Structure

```
storage-labels-api/
├── Authorization/          # Custom authorization handlers
│   ├── HasScopeHandler.cs        # Permission-based authorization
│   └── HasScopeRequirement.cs    # Permission requirements
├── Datalayer/             # Database context and models
│   ├── StorageLabelsDbContext.cs
│   └── Models/                   # Database entities
├── Endpoints/             # HTTP endpoint definitions (Minimal APIs)
│   ├── MapAll.cs                 # Registers all endpoint groups
│   ├── MapAuthentication.cs      # Auth endpoints
│   ├── MapUser.cs                # User management endpoints
│   ├── MapLocation.cs            # Location endpoints
│   └── ...
├── Extensions/            # Extension methods
│   ├── HttpContextExtensions.cs  # Extract user info from context
│   └── UserIdHasher.cs           # User ID hashing utilities
├── Filters/               # Endpoint filters
│   ├── UserExistsFilter.cs       # Validates user existence
│   └── OperationCancelledExceptionFilter.cs
├── Handlers/              # Business logic handlers (Mediator)
│   ├── Authentication/           # Login, Register, etc.
│   ├── Users/                    # User CRUD operations
│   ├── Locations/                # Location management
│   ├── Boxes/                    # Box management
│   ├── Items/                    # Item management
│   └── Images/                   # Image upload/encryption
├── Logging/               # Structured logging definitions
│   ├── LogMessages.cs            # Main log messages
│   ├── LogMessages.User.cs       # User-related logs
│   └── ...
├── Migrations/            # EF Core migrations
├── Models/                # DTOs and settings
│   ├── DTO/                      # Data Transfer Objects
│   └── Settings/                 # Configuration models
├── Services/              # Application services
│   ├── Authentication/           # Auth services
│   ├── JwtTokenService.cs        # JWT generation
│   └── RoleInitializationService.cs
├── Transformer/           # OpenAPI transformers
└── Program.cs             # Application entry point
```

## Design Patterns

### 1. Mediator Pattern (Request/Handler)

**Purpose**: Decouple HTTP endpoints from business logic

**Structure**:
```csharp
// Request (in Handler file)
public record CreateItem(
    string UserId,
    Guid BoxId,
    string Name,
    string? Description
) : IRequest<Result<Item>>;

// Handler
public class CreateItemHandler(
    StorageLabelsDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<CreateItemHandler> logger
) : IRequestHandler<CreateItem, Result<Item>>
{
    public async ValueTask<Result<Item>> Handle(CreateItem request, CancellationToken cancellationToken)
    {
        // Business logic here
        var validation = await new CreateItemValidator().ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Result<Item>.Invalid(validation.AsErrors());
        }
        
        // ... create and save entity
        
        return Result.Success(item);
    }
}
```

**Benefits**:
- Single responsibility per handler
- Easy to test in isolation
- Clear request/response contracts
- Middleware-free business logic

### 2. Result Pattern

**Purpose**: Railway-oriented error handling without exceptions for business logic failures

**Usage**:
```csharp
// Success
return Result.Success(data);
return Result<Item>.Success(item);

// Not Found
return Result.NotFound("User not found");

// Validation Error
return Result.Invalid(validationErrors);

// Business Logic Error
return Result.Error("Operation failed");

// Forbidden
return Result.Forbidden();

// In endpoints, convert to HTTP responses
return result.ToMinimalApiResult();
```

**Result Types**:
- `Result` - No data returned
- `Result<T>` - Returns data on success
- Status checks: `result.IsSuccess`, `result.IsNotFound`, `result.IsInvalid`, etc.

### 3. Primary Constructor Pattern (C# 12)

**Purpose**: Concise dependency injection

```csharp
public class CreateItemHandler(
    StorageLabelsDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<CreateItemHandler> logger
) : IRequestHandler<CreateItem, Result<Item>>
{
    // Fields automatically created from constructor parameters
    // Use dbContext, timeProvider, logger directly
}
```

### 4. Record Types for DTOs

**Purpose**: Immutable data contracts

```csharp
public record LoginRequest(string UsernameOrEmail, string Password, bool RememberMe = false);
public record AuthenticationResult(string Token, DateTime ExpiresAt, UserInfoResponse User);
public record UpdateUserRoleRequest(string Role);
```

**Benefits**:
- Value equality
- Immutability
- Concise syntax
- With-expressions for non-destructive mutations

## Data Flow

### Request Flow (Create Item Example)

```
1. HTTP Request
   POST /api/location/{locationId}/box/{boxId}/item
   Body: { "name": "Screwdriver", "description": "Phillips head" }
   
2. Endpoint (MapItem.cs)
   ↓ Extract user ID from JWT claims
   ↓ Apply authorization filter (requires Edit permission)
   ↓ Map request to CreateItem record
   
3. Mediator
   ↓ Send CreateItem request to handler
   
4. Handler (CreateItemHandler.cs)
   ↓ Validate user has Edit access to the box
   ↓ Validate request with FluentValidation
   ↓ Create Item entity
   ↓ Save to database via DbContext
   ↓ Log success
   ↓ Return Result<Item>
   
5. Endpoint
   ↓ Convert Result to HTTP response (200 OK or 400/404)
   ↓ Serialize Item to JSON
   
6. HTTP Response
   { "itemId": "...", "name": "Screwdriver", ... }
```

### Authentication Flow

```
1. Login Request
   POST /api/auth/login
   Body: { "usernameOrEmail": "admin", "password": "***" }
   
2. LoginHandler
   ↓ Validate credentials with ASP.NET Core Identity
   ↓ Get user roles from RoleManager
   ↓ Map roles to permissions
   
3. JwtTokenService
   ↓ Create claims (sub, email, permissions, roles)
   ↓ Generate JWT token
   ↓ Set expiration
   
4. Response
   { "token": "eyJ...", "expiresAt": "...", "user": {...} }
   
5. Subsequent Requests
   Authorization: Bearer eyJ...
   ↓ JWT middleware validates token
   ↓ Claims populated in HttpContext.User
   ↓ Permission checks via HasScopeHandler
```

### Authorization Flow

```
1. Endpoint requires permission
   .RequireAuthorization(Policies.Write_Item)
   
2. Request arrives with JWT token
   
3. HasScopeHandler
   ↓ Extract "permissions" claim from token
   ↓ Check if required permission exists
   ↓ Return AuthorizationResult.Success() or Fail()
   
4. If authorized → Handler executes
   If unauthorized → 403 Forbidden response
```

## Authentication & Authorization

### Authentication Modes

**Local Authentication** (Default):
- ASP.NET Core Identity for user management
- JWT tokens for stateless authentication
- Password hashing with PBKDF2
- Configurable password requirements
- Account lockout protection

**No Authentication**:
- All requests bypass authentication
- All users get full permissions
- For trusted networks only

### Roles & Permissions

**Roles**:
- `Admin` - Full system access
- `Auditor` - Read-only access
- `User` - Standard access

**Permission Mapping** (in `LocalAuthenticationService.cs` and `RoleInitializationService.cs`):
```csharp
private static IEnumerable<string> GetPermissionsForRole(string role) => role switch
{
    "Admin" => new[]
    {
        "read:user", "write:user",
        "read:common-locations", "write:common-locations",
        "read:encryption-keys", "write:encryption-keys"
    },
    "Auditor" => new[]
    {
        "read:user",
        "read:common-locations",
        "read:encryption-keys"
    },
    "User" => Array.Empty<string>(),
    _ => Array.Empty<string>()
};
```

**Policy Constants** (in `Models/Policies.cs`):
```csharp
public static class Policies
{
    public const string Read_User = "read:user";
    public const string Write_User = "write:user";
    public const string Read_CommonLocations = "read:common-locations";
    public const string Write_CommonLocations = "write:common-locations";
    public const string Read_EncryptionKeys = "read:encryption-keys";
    public const string Write_EncryptionKeys = "write:encryption-keys";
}
```

### First User Auto-Elevation

The first registered user automatically receives the `Admin` role (see `LocalAuthenticationService.cs`):
```csharp
var userCount = await _userManager.Users.CountAsync(cancellationToken);
var roleToAssign = userCount == 1 ? "Admin" : "User";
await _userManager.AddToRoleAsync(user, roleToAssign);
```

### Extracting User Context

```csharp
// In endpoint handlers
var userId = context.GetUserId();        // Throws if not authenticated
var userId = context.TryGetUserId();     // Returns null if not authenticated

// Extension method implementation (HttpContextExtensions.cs)
public static string GetUserId(this HttpContext context)
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? context.User.FindFirst("sub")?.Value;
    
    if (string.IsNullOrWhiteSpace(userId))
    {
        throw new UnauthorizedAccessException("User ID not found in claims");
    }
    
    return userId;
}
```

## Logging

### Structured Logging with Source Generators

**Location**: `Logging/` directory

**Pattern**:
```csharp
// LogMessages.Item.cs
public static partial class LogMessages
{
    [LoggerMessage(
        Message = "User ({userId}) attempted to add item to box ({boxId}) without permission.",
        Level = LogLevel.Warning
    )]
    public static partial void LogItemAddAttemptWarning(
        this ILogger logger,
        string userId,
        Guid boxId
    );
    
    [LoggerMessage(
        Message = "Item ({itemId}) created successfully in box ({boxId}).",
        Level = LogLevel.Information
    )]
    public static partial void LogItemCreated(
        this ILogger logger,
        Guid itemId,
        Guid boxId
    );
}
```

**Usage in Handlers**:
```csharp
public class CreateItemHandler(ILogger<CreateItemHandler> logger) : ...
{
    public async ValueTask<Result<Item>> Handle(...)
    {
        if (!userCanEditBox)
        {
            logger.LogItemAddAttemptWarning(request.UserId, request.BoxId);
            return Result.Invalid(...);
        }
        
        // ... create item
        
        logger.LogItemCreated(item.ItemId, item.BoxId);
        return Result.Success(item);
    }
}
```

**Benefits**:
- Compile-time validation of log parameters
- High performance (no boxing, no allocations)
- Consistent log message structure
- Easy to search and filter logs

### Log Levels

- `Trace`: Very detailed debugging
- `Debug`: Debugging information
- `Information`: General flow (item created, user logged in)
- `Warning`: Unexpected behavior (invalid access attempts)
- `Error`: Operation failures (database errors)
- `Critical`: System failures

## Database

### Entity Framework Core

**DbContext**: `StorageLabelsDbContext.cs`

**Entity Conventions**:
- Primary keys: `{EntityName}Id` (e.g., `UserId`, `BoxId`)
- Use `Guid` (CreateVersion7) for IDs when possible
- Timestamps: `Created`, `Updated` (UTC)
- Soft deletes: Not used (hard deletes)

**Table Naming**:
- PostgreSQL lowercase convention: `users`, `locations`, `boxes`, `items`
- Configured in `OnModelCreating`:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>().ToTable("users");
    modelBuilder.Entity<Location>().ToTable("locations");
    // ...
}
```

### Migrations

**Create Migration**:
```bash
dotnet ef migrations add MigrationName --project storage-labels-api
```

**Apply Migration**:
```bash
dotnet ef database update --project storage-labels-api
```

**Current Migration**: `SimplifiedLocalAuthentication` - Migrated from Auth0 to ASP.NET Core Identity

### Database Models

Two types of models:

1. **Database Entities** (`DataLayer/Models/`)
   - EF Core entities with navigation properties
   - Example: `User`, `Location`, `Box`, `Item`

2. **Identity Models** (`Datalayer/Models/`)
   - ASP.NET Core Identity: `ApplicationUser`
   - Separate from business entities

### Relationships

- `User` ←→ `UserLocation` ←→ `Location` (Many-to-Many with access level)
- `Location` → `Box` (One-to-Many)
- `Box` → `Item` (One-to-Many)
- `Item` → `ImageMetadata` (One-to-One, nullable)

## Error Handling

### Validation

**FluentValidation** for request validation:

```csharp
public class CreateItemValidator : AbstractValidator<CreateItem>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
            
        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}
```

**Usage in Handler**:
```csharp
var validation = await new CreateItemValidator().ValidateAsync(request);
if (!validation.IsValid)
{
    return Result<Item>.Invalid(validation.AsErrors());
}
```

### Exception Filters

**OperationCancelledExceptionFilter** (`Filters/`):
- Catches `OperationCanceledException`
- Returns `499 Client Closed Request` status
- Prevents exception logging for cancelled requests

**Applied Globally**:
```csharp
builder.Services.AddExceptionHandler<OperationCancelledExceptionFilter>();
```

### Result Pattern Error Handling

```csharp
// In handlers
return Result.NotFound($"Item {id} not found");
return Result.Invalid(validationErrors);
return Result.Error("Database connection failed");
return Result.Forbidden();

// In endpoints - automatic HTTP status mapping
return result.ToMinimalApiResult();
// Success → 200 OK
// NotFound → 404 Not Found
// Invalid → 400 Bad Request
// Error → 500 Internal Server Error
// Forbidden → 403 Forbidden
```

## Code Conventions

### Naming

- **Handlers**: `{Action}{Entity}Handler` (e.g., `CreateItemHandler`)
- **Requests**: `{Action}{Entity}` (e.g., `CreateItem`)
- **DTOs**: `{Entity}Response`, `{Entity}Request` (e.g., `ItemResponse`)
- **Endpoints**: `Map{Entity}` (e.g., `MapItem`)
- **Services**: `{Purpose}Service` (e.g., `JwtTokenService`)

### File Organization

- One handler per file
- Handler and request in same file
- Validators in same file as handler
- Group by feature (Items/, Users/, Locations/)

### Async Conventions

- Use `async`/`await` for I/O operations
- Return `ValueTask<>` in handlers (lighter than Task)
- Accept `CancellationToken cancellationToken` parameter
- Use `ConfigureAwait(false)` NOT needed in ASP.NET Core

### Null Handling

- Enable nullable reference types (project-wide)
- Use `?` for nullable properties
- Use `??` for null coalescing
- Use `?.` for null-conditional operators

## Adding New Features

### 1. Create Handler

```csharp
// Handlers/Items/DeleteItem.cs
namespace StorageLabelsApi.Handlers.Items;

public record DeleteItem(string UserId, Guid ItemId) : IRequest<Result>;

public class DeleteItemHandler(
    StorageLabelsDbContext dbContext,
    ILogger<DeleteItemHandler> logger
) : IRequestHandler<DeleteItem, Result>
{
    public async ValueTask<Result> Handle(DeleteItem request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .FirstOrDefaultAsync(i => i.ItemId == request.ItemId, cancellationToken);
            
        if (item == null)
        {
            return Result.NotFound($"Item {request.ItemId} not found");
        }
        
        // Check permissions...
        
        dbContext.Items.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogItemDeleted(request.ItemId);
        return Result.Success();
    }
}
```

### 2. Add Endpoint

```csharp
// Endpoints/MapItem.cs
routeBuilder.MapDelete("/{itemId:guid}", DeleteItem)
    .RequireAuthorization() // Add permission if needed
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("Delete Item");

private static async Task<IResult> DeleteItem(
    HttpContext context,
    Guid itemId,
    [FromServices] IMediator mediator,
    CancellationToken cancellationToken)
{
    var userId = context.GetUserId();
    var result = await mediator.Send(new Handlers.Items.DeleteItem(userId, itemId), cancellationToken);
    return result.ToMinimalApiResult();
}
```

### 3. Add Logging

```csharp
// Logging/LogMessages.Item.cs
[LoggerMessage(
    Message = "Item ({itemId}) deleted by user ({userId}).",
    Level = LogLevel.Information
)]
public static partial void LogItemDeleted(
    this ILogger logger,
    Guid itemId
);
```

### 4. Add Tests (if applicable)

```csharp
// Tests/Handlers/Items/DeleteItemHandlerTests.cs
[Fact]
public async Task Handle_ItemExists_DeletesItem()
{
    // Arrange
    var handler = new DeleteItemHandler(dbContext, logger);
    var request = new DeleteItem(userId, itemId);
    
    // Act
    var result = await handler.Handle(request, CancellationToken.None);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

## Configuration

### appsettings.json

```json
{
  "Authentication": {
    "Mode": "Local",  // or "None"
    "Local": {
      "AllowRegistration": true,
      "MinimumPasswordLength": 8,
      "RequireDigit": true,
      "RequireLowercase": true,
      "RequireUppercase": true,
      "RequireNonAlphanumeric": false,
      "LockoutEnabled": true,
      "MaxFailedAccessAttempts": 5,
      "LockoutDurationMinutes": 15
    }
  },
  "Jwt": {
    "Secret": "your-secret-min-32-chars",
    "Issuer": "storage-labels-api",
    "Audience": "storage-labels-ui",
    "ExpirationMinutes": 60
  }
}
```

**Note**: If the JWT secret is missing or contains a placeholder value (e.g., "your-secret-min-32-chars", "change-in-production"), the application will automatically generate a cryptographically secure random secret on startup. However, this secret will change on each restart, invalidating all existing tokens. For production, always set a persistent secret.

### JWT Secret Auto-Generation

The API automatically generates a secure JWT secret if:
- The `Jwt:Secret` configuration is empty or missing
- The secret contains placeholder text like "your-secret" or "change-in-production"

**On startup with auto-generation, you'll see**:
```
⚠️  JWT Secret was not configured or is a placeholder. A random secret has been generated.
⚠️  This secret will change on every restart, invalidating all existing tokens.
⚠️  For production, set a persistent JWT secret via environment variable or appsettings.json
Generated JWT Secret (save this for persistence): [64-character base64 string]
```

**To persist the generated secret**:
1. Copy the generated secret from the startup logs
2. Set it via environment variable: `Jwt__Secret=<copied-secret>`
3. Or add it to `appsettings.json` (not recommended for production)

**Generate a secret manually**:
```bash
# Using OpenSSL
openssl rand -base64 64

# Using PowerShell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

# Using Python
python -c "import secrets; print(secrets.token_urlsafe(64))"
```

### Environment Variables

Override via environment variables:
- `Authentication__Mode`
- `Jwt__Secret` (highly recommended for production)
- `ConnectionStrings__DefaultConnection`

## Development

### Running Locally

```bash
# Restore dependencies
dotnet restore

# Apply migrations
dotnet ef database update

# Run
dotnet run
```

### Swagger/OpenAPI

Available at: `http://localhost:5000/openapi/v1.json`

Swagger UI: `http://localhost:5000/swagger`

### Docker

```bash
docker build -t storage-labels-api .
docker run -p 8080:8080 storage-labels-api
```

## Best Practices

1. **Always use Result pattern** - Don't throw exceptions for business logic failures
2. **Log important events** - Use structured logging with source generators
3. **Validate early** - Use FluentValidation before business logic
4. **Check permissions** - Validate user access before operations
5. **Use primary constructors** - Cleaner dependency injection
6. **Accept CancellationToken** - Support request cancellation
7. **Use records for DTOs** - Immutable data contracts
8. **Keep handlers focused** - Single responsibility per handler
9. **Test handlers in isolation** - Mock DbContext and dependencies
10. **Use TimeProvider** - Testable time operations

## Resources

- [Mediator Documentation](https://github.com/martinothamar/Mediator)
- [Ardalis.Result](https://github.com/ardalis/Result)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [EF Core](https://learn.microsoft.com/en-us/ef/core/)
