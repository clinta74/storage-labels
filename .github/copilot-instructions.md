# GitHub Copilot Instructions for Storage Labels

## Critical Coding Patterns - MUST FOLLOW

### 1. DTO Pattern (Response DTOs)
```csharp
// ✅ CORRECT - Use constructor mapping
public record ItemResponse(int Id, string Name, ...) 
{
    public ItemResponse(ItemModel item) : this(item.Id, item.Name, ...) { }
}

// Usage in endpoints/handlers:
items.Select(item => new ItemResponse(item))

// ❌ WRONG - Don't use static methods
public static ItemResponse FromEntity(ItemModel item) { ... }

// ❌ WRONG - Don't manually map properties
new ItemResponse { Id = item.Id, Name = item.Name, ... }
```

### 2. MediatR Handler Pattern
```csharp
// ✅ CORRECT - Request as record
public record CreateItem(string UserId, string Name, int BoxId) 
    : IRequest<Result<ItemResponse>>;

// ✅ CORRECT - Handler implements IRequestHandler
public class CreateItemHandler : IRequestHandler<CreateItem, Result<ItemResponse>>
{
    public async ValueTask<Result<ItemResponse>> Handle(...)
    {
        // Business logic here
        return Result.Success(new ItemResponse(item));
    }
}
```

### 3. Validation Pattern
```csharp
// ✅ CORRECT - Separate validator class
public class CreateItemValidator : AbstractValidator<CreateItem>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BoxId).GreaterThan(0);
    }
}
```

### 4. Logging Pattern
```csharp
// ✅ CORRECT - LoggerMessage source generator
public static partial class LogMessages
{
    [LoggerMessage(Message = "User {userId} created item {itemId}", Level = LogLevel.Information)]
    public static partial void ItemCreated(this ILogger logger, string userId, int itemId);
}

// Usage: logger.ItemCreated(userId, itemId);

// ❌ WRONG - String interpolation
logger.LogInformation($"User {userId} created item {itemId}");
```

### 5. Endpoint Mapping Pattern
```csharp
// ✅ CORRECT - Route constraints and version suffixes
group.MapGet("/{itemId:int}", GetItemById)
    .WithName("Get Item By ID V2")  // Include version suffix
    .Produces<ItemResponse>(StatusCodes.Status200OK);

// ❌ WRONG - No constraints, duplicate names
group.MapGet("/{itemId}", GetItemById)
    .WithName("Get Item By ID")
```

### 6. Route Parameters
```csharp
// ✅ CORRECT - camelCase with type constraints
"/{boxId:guid}"
"/{locationId:long}"
"/{itemId:int}"

// ❌ WRONG - kebab-case
"/{box-id}"
```

## File Organization

- **DTOs**: `Models/DTO/FeatureName/` (e.g., `Models/DTO/Item/ItemResponse.cs`)
- **Internal Models**: `Models/FeatureName/` (e.g., `Models/Search/SearchResult.cs`)
- **Handlers**: `Handlers/FeatureName/` (e.g., `Handlers/Items/CreateItemHandler.cs`)
- **Validators**: Same file as handler or separate if complex
- **Endpoints**: `Endpoints/MapFeatureName.cs` (e.g., `Endpoints/MapItem.cs`)
- **Logging**: `Logging/LogMessages.FeatureName.cs`

## Before Generating Code

1. **Search for existing patterns** - Use similar features as templates
2. **Check CONTRIBUTING.md** - Detailed patterns in Architecture & Patterns section
3. **Test immediately** - Run `dotnet test` after changes
4. **Follow namespace conventions** - Match existing file structure

## Common Mistakes to Avoid

- ❌ Using static factory methods on DTOs instead of constructors
- ❌ Mixing DTOs and internal models in the same namespace
- ❌ Manual property mapping instead of constructor mapping
- ❌ Validating in handlers instead of validators
- ❌ String interpolation in logging instead of LoggerMessage
- ❌ Duplicate endpoint names across versions
- ❌ Missing type constraints on route parameters
