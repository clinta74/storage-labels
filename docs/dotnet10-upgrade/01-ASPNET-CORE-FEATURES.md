# ASP.NET Core 10 Features Guide

## Overview
This guide covers the major ASP.NET Core 10 improvements including Native AOT readiness, enhanced minimal APIs, improved OpenAPI generation, built-in rate limiting, and keyed dependency injection.

---

## 1. Native AOT (Ahead-of-Time Compilation)

### What It Is
Native AOT compiles your application to native machine code during build time, eliminating the need for JIT (Just-In-Time) compilation at runtime. This results in faster startup, smaller memory footprint, and smaller deployment size.

### Benefits

| Metric | Traditional JIT | Native AOT | Improvement |
|--------|----------------|------------|-------------|
| **Startup Time** | 1000-2000ms | 50-150ms | 10-20x faster |
| **Memory Usage** | 80-120 MB | 15-30 MB | 3-4x less |
| **Deployment Size** | 85 MB (with runtime) | 10-15 MB | 5-6x smaller |
| **Cold Start (AWS Lambda)** | 2-5 seconds | 200-500ms | 10x faster |

### How It Works

```
Traditional JIT Compilation:
1. Publish .NET IL code
2. Runtime included in deployment
3. At startup: JIT compiles IL → native code
4. Application runs

Native AOT:
1. During build: Compile IL → native code
2. No runtime needed
3. At startup: Execute native code immediately
4. Application runs
```

### Compatibility Requirements

**✅ Compatible with Storage Labels API:**
- Minimal APIs ✓
- ASP.NET Core Identity ✓
- Entity Framework Core ✓
- System.Text.Json ✓
- Dependency Injection ✓

**⚠️ Requires Changes:**
- Remove reflection-based features
- Use source generators instead of runtime reflection
- JSON serialization context required
- No dynamic assembly loading

**❌ Not Compatible:**
- Newtonsoft.Json (use System.Text.Json)
- Runtime code generation
- Dynamic proxy generation (Moq for testing)
- Some third-party libraries

### Implementation for Storage Labels API

#### Step 1: Enable Native AOT

**Update `storage-labels-api.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>false</InvariantGlobalization>
    <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
  </PropertyGroup>
</Project>
```

#### Step 2: Create JSON Source Generation Context

**Create `JsonContext.cs`:**
```csharp
using System.Text.Json.Serialization;
using StorageLabelsApi.Models.DTO;

namespace StorageLabelsApi;

[JsonSerializable(typeof(BoxResponse))]
[JsonSerializable(typeof(List<BoxResponse>))]
[JsonSerializable(typeof(ItemResponse))]
[JsonSerializable(typeof(List<ItemResponse>))]
[JsonSerializable(typeof(SearchResultsResponse))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(Result<BoxResponse>))]
[JsonSerializable(typeof(Result<List<BoxResponse>>))]
// Add all DTOs used in API
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
public partial class StorageLabelsJsonContext : JsonSerializerContext
{
}
```

**Register in `Program.cs`:**
```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, StorageLabelsJsonContext.Default);
});
```

#### Step 3: Update EF Core for AOT

**In `Program.cs`:**
```csharp
// Enable compiled model for AOT
builder.Services.AddDbContext<StorageLabelsDbContext>(options =>
{
    options.UseNpgsql(connectionString)
        .UseModel(CompiledModels.StorageLabelsDbContextModel.Instance); // Pre-compiled model
});
```

**Generate compiled model:**
```bash
dotnet ef dbcontext optimize --output-dir CompiledModels --namespace StorageLabelsApi.CompiledModels
```

#### Step 4: Fix Mediator for AOT

**Current issue:** Mediator uses reflection for handler discovery

**Solution:** Use source generators (already done with `Mediator.SourceGenerator`)

```csharp
// Already AOT-compatible in your project!
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
```

#### Step 5: Build and Test

```bash
# Analyze AOT compatibility
dotnet publish -c Release /p:PublishAot=true

# Check warnings (IL2026, IL2050, IL2060, IL2070, IL3050)
# Fix any incompatibilities

# Test native executable
cd bin/Release/net10.0/win-x64/publish
.\storage-labels-api.exe
```

### AOT Warnings and Fixes

**Common Warning: IL2026 (Reflection)**
```csharp
// ❌ Problem: Runtime reflection
var type = Type.GetType("StorageLabelsApi.Handlers.BoxHandler");
var method = type.GetMethod("Handle");

// ✅ Solution: Use source generators or explicit registration
// Already solved with Mediator.SourceGenerator
```

**Common Warning: IL2050 (Dynamic code)**
```csharp
// ❌ Problem: Expression compilation
Expression<Func<Box, bool>> expr = b => b.Name == name;
var compiled = expr.Compile(); // Not AOT-compatible

// ✅ Solution: Use direct delegates
Func<Box, bool> predicate = b => b.Name == name;
```

### Docker Support for Native AOT

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy and restore
COPY ["storage-labels-api/storage-labels-api.csproj", "storage-labels-api/"]
RUN dotnet restore "storage-labels-api/storage-labels-api.csproj"

# Build Native AOT
COPY . .
WORKDIR "/src/storage-labels-api"
RUN dotnet publish -c Release -o /app/publish /p:PublishAot=true

# Runtime image - much smaller!
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

# No .NET runtime needed!
ENTRYPOINT ["./storage-labels-api"]
```

**Image sizes:**
- With runtime: ~200 MB
- Native AOT: ~35 MB
- **5.7x smaller!**

### When to Use Native AOT

✅ **Use When:**
- Serverless/Lambda functions (cold start critical)
- Microservices with many instances (memory savings)
- Edge computing / IoT scenarios
- Container environments (smaller images)
- Startup time is critical

❌ **Don't Use When:**
- Heavy reflection usage
- Many incompatible third-party libraries
- Development/debugging (slower compile times)
- Build complexity not worth benefits

### Gradual Adoption Strategy

1. **Analyze readiness:** Run `dotnet publish /p:PublishAot=true` and review warnings
2. **Fix high-priority warnings:** JSON serialization, EF Core
3. **Test in staging:** Verify all features work
4. **Monitor metrics:** Startup time, memory, CPU
5. **Production rollout:** Deploy to subset of instances first

---

## 2. Enhanced Minimal API Analyzers

### What It Is
.NET 10 includes improved compile-time analyzers that detect issues in minimal API endpoints, providing better type safety and preventing runtime errors.

### New Analyzers in .NET 10

#### Analyzer 1: Route Parameter Type Mismatch

**Detected at compile time:**
```csharp
// ❌ ERROR: Parameter type mismatch
app.MapGet("/boxes/{id}", (string id, StorageLabelsDbContext db) => 
{
    var boxId = Guid.Parse(id); // Analyzer warns: Route says Guid, parameter is string
    // ...
});

// ✅ CORRECT: Types match
app.MapGet("/boxes/{id:guid}", (Guid id, StorageLabelsDbContext db) => 
{
    // Type-safe, no parsing needed
});
```

#### Analyzer 2: Missing Route Parameters

```csharp
// ❌ ERROR: Route requires {locationId} but handler doesn't have it
app.MapGet("/locations/{locationId}/boxes", (StorageLabelsDbContext db) => 
{
    // Analyzer error: Missing required parameter 'locationId'
});

// ✅ CORRECT
app.MapGet("/locations/{locationId}/boxes", (long locationId, StorageLabelsDbContext db) => 
{
    return db.Boxes.Where(b => b.LocationId == locationId);
});
```

#### Analyzer 3: Ambiguous Injection

```csharp
// ❌ WARNING: Ambiguous - is this from route, query, or DI?
app.MapGet("/search", (string query, string userId) => 
{
    // Both could be query parameters OR injected services
});

// ✅ CORRECT: Explicit binding
app.MapGet("/search", 
    ([FromQuery] string query, [FromServices] IUserService userService) => 
{
    var userId = userService.GetCurrentUserId();
    // Clear intent
});
```

#### Analyzer 4: Return Type Analysis

```csharp
// ❌ WARNING: Inconsistent return types
app.MapGet("/boxes/{id:guid}", async (Guid id, StorageLabelsDbContext db) => 
{
    var box = await db.Boxes.FindAsync(id);
    if (box == null)
        return Results.NotFound(); // Returns IResult
    return box; // Returns Box - inconsistent!
});

// ✅ CORRECT: Consistent return type
app.MapGet("/boxes/{id:guid}", async Task<Results<Ok<BoxResponse>, NotFound>> 
    (Guid id, IMediator mediator) => 
{
    var result = await mediator.Send(new GetBoxQuery(id));
    return result.IsSuccess 
        ? TypedResults.Ok(result.Value)
        : TypedResults.NotFound();
});
```

### Implementation in Storage Labels API

#### Update MapBox.cs with Type Safety

**Current code:**
```csharp
public static void MapBoxEndpoints(this IEndpointRouteBuilder app)
{
    var boxes = app.MapGroup("/api/boxes")
        .WithTags("Boxes")
        .RequireAuthorization();

    boxes.MapGet("/{id}", async (Guid id, IMediator mediator, HttpContext httpContext) =>
    {
        var result = await mediator.Send(new GetBoxQuery(id, httpContext.GetUserId()));
        return result.ToMinimalApiResult();
    });
}
```

**Enhanced with analyzers:**
```csharp
public static void MapBoxEndpoints(this IEndpointRouteBuilder app)
{
    var boxes = app.MapGroup("/api/boxes")
        .WithTags("Boxes")
        .RequireAuthorization();

    // Typed result with compile-time checking
    boxes.MapGet("/{id:guid}", async Task<Results<Ok<BoxResponse>, NotFound, ValidationProblem>>
        ([FromRoute] Guid id, 
         [FromServices] IMediator mediator, 
         [FromServices] HttpContext httpContext) =>
    {
        var result = await mediator.Send(new GetBoxQuery(id, httpContext.GetUserId()));
        
        return result.Status switch
        {
            ResultStatus.Ok => TypedResults.Ok(result.Value),
            ResultStatus.NotFound => TypedResults.NotFound(),
            ResultStatus.Invalid => TypedResults.ValidationProblem(result.ValidationErrors.ToDictionary()),
            _ => throw new InvalidOperationException()
        };
    })
    .Produces<BoxResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);
}
```

#### Analyzer-Friendly Patterns

```csharp
// Pattern 1: Explicit route constraints
app.MapGet("/locations/{locationId:long}/boxes/{boxId:guid}", 
    (long locationId, Guid boxId) => { });

// Pattern 2: Named parameters matching route exactly
app.MapPost("/items", async (CreateItemRequest request, IMediator mediator) => 
{
    // Request body binding is clear
});

// Pattern 3: Use [AsParameters] for complex bindings
public record SearchParameters(
    [FromQuery] string Query,
    [FromQuery] long? LocationId,
    [FromQuery] Guid? BoxId
);

app.MapGet("/search", async ([AsParameters] SearchParameters params, IMediator mediator) =>
{
    return await mediator.Send(new SearchQuery(params.Query, params.LocationId, params.BoxId));
});
```

### Benefits

- **Compile-time safety:** Catch errors before deployment
- **Better IDE support:** IntelliSense shows route parameters
- **Clearer intent:** Explicit binding attributes
- **Reduced bugs:** Type mismatches caught early

---

## 3. Improved OpenAPI Generation

### What It Is
.NET 10 includes enhanced OpenAPI generation with better support for complex types, inheritance, and minimal API patterns.

### Improvements Over .NET 9

| Feature | .NET 9 | .NET 10 |
|---------|--------|---------|
| Polymorphic types | ❌ Manual config | ✅ Automatic |
| Union types (Results\<T1, T2\>) | ⚠️ Limited | ✅ Full support |
| Required properties | ⚠️ Inconsistent | ✅ Accurate |
| Endpoint descriptions | Manual | ✅ XML comments |
| Response examples | Manual | ✅ Auto-generated |

### Implementation

#### Step 1: Enhanced Endpoint Documentation

**Before:**
```csharp
boxes.MapGet("/{id}", async (Guid id, IMediator mediator) =>
{
    // OpenAPI generation is limited
});
```

**After (.NET 10):**
```csharp
boxes.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetBoxQuery(id));
    return result.ToMinimalApiResult();
})
.WithName("GetBox")
.WithSummary("Get a box by ID")
.WithDescription("Retrieves a single box with all its items and location information")
.WithOpenApi(operation =>
{
    operation.Parameters[0].Description = "The unique identifier of the box";
    return operation;
})
.Produces<BoxResponse>(StatusCodes.Status200OK, "application/json", "Box found")
.Produces(StatusCodes.Status404NotFound, "Box not found")
.Produces(StatusCodes.Status401Unauthorized, "User not authenticated");
```

#### Step 2: Result\<T\> Support

**Create OpenAPI transformer for Ardalis.Result:**

```csharp
public class ResultOpenApiTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(
        OpenApiOperation operation, 
        OpenApiOperationTransformerContext context, 
        CancellationToken cancellationToken)
    {
        // Check if return type is Result<T>
        var returnType = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IProducesResponseTypeMetadata>()
            .FirstOrDefault()?.Type;

        if (returnType?.IsGenericType == true && 
            returnType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = returnType.GetGenericArguments()[0];
            
            // Add 200 response with success value
            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "Success",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(valueType, context.SchemaRepository)
                    }
                }
            };
            
            // Add error responses
            operation.Responses.TryAdd("400", new OpenApiResponse 
            { 
                Description = "Validation error",
                Content = GenerateProblemDetailsSchema(context)
            });
            operation.Responses.TryAdd("404", new OpenApiResponse 
            { 
                Description = "Not found" 
            });
        }

        await Task.CompletedTask;
    }
}

// Register in Program.cs
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<ResultOpenApiTransformer>();
});
```

#### Step 3: XML Documentation

**Enable in `storage-labels-api.csproj`:**
```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn> <!-- Disable missing XML comment warnings -->
</PropertyGroup>
```

**Add XML comments:**
```csharp
/// <summary>
/// Retrieves a box by its unique identifier
/// </summary>
/// <param name="id">The unique identifier of the box (GUID)</param>
/// <param name="mediator">Mediator for CQRS pattern</param>
/// <param name="httpContext">HTTP context for user identification</param>
/// <returns>Box details including items and location</returns>
/// <response code="200">Returns the requested box</response>
/// <response code="404">If the box doesn't exist or user lacks access</response>
/// <response code="401">If the user is not authenticated</response>
boxes.MapGet("/{id:guid}", async (Guid id, IMediator mediator, HttpContext httpContext) =>
{
    // Implementation
});
```

#### Step 4: Example Responses

```csharp
boxes.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
{
    // ...
})
.WithOpenApi(operation =>
{
    operation.Responses["200"].Content["application/json"].Example = new OpenApiString(@"
    {
        ""boxId"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
        ""name"": ""Kitchen Supplies"",
        ""code"": ""KIT-001"",
        ""description"": ""Pots, pans, and utensils"",
        ""locationId"": 1,
        ""locationName"": ""Garage"",
        ""items"": [
            {
                ""itemId"": ""1fa85f64-5717-4562-b3fc-2c963f66afa7"",
                ""name"": ""Large Pot"",
                ""quantity"": 1
            }
        ]
    }");
    
    return operation;
});
```

### Swagger UI Enhancements

**In `Program.cs`:**
```csharp
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // New .NET 10 endpoint
    
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Storage Labels API v1");
        options.RoutePrefix = "swagger";
        
        // .NET 10 enhancements
        options.DisplayRequestDuration = true;
        options.EnableDeepLinking = true;
        options.EnableFilter = true;
        options.ShowExtensions = true;
        options.EnableValidator = true;
        
        // Custom CSS for better readability
        options.InjectStylesheet("/swagger-ui/custom.css");
    });
}
```

---

## 4. Built-In Rate Limiting

### What It Is
ASP.NET Core 10 includes enhanced rate limiting middleware with better performance, more algorithms, and per-user tracking.

### Rate Limiting Algorithms

| Algorithm | Use Case | How It Works |
|-----------|----------|--------------|
| **Fixed Window** | Simple rate limiting | 100 req/minute, resets every minute |
| **Sliding Window** | Smoother distribution | Rolling 60-second window |
| **Token Bucket** | Burst handling | Accumulate tokens, spend on requests |
| **Concurrency** | Limit simultaneous requests | Max N concurrent requests |

### Current Implementation

**Your custom rate limiter** (`RateLimitFilter.cs`):
```csharp
// Custom implementation in EndpointFilter
public class RateLimitFilter : IEndpointFilter
{
    // 100 requests per minute
    private static readonly int MaxRequests = 100;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);
    // ... manual tracking
}
```

### Built-In Rate Limiting (.NET 10)

**Remove custom filter, use built-in:**

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    // Default policy: Sliding window
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        
        return RateLimitPartition.GetSlidingWindowLimiter(userId, key => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6, // Check every 10 seconds
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 10
        });
    });
    
    // Policy for authentication endpoints (stricter)
    options.AddPolicy("auth", context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        return RateLimitPartition.GetFixedWindowLimiter(ipAddress, key => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0 // No queueing for auth
        });
    });
    
    // Policy for search (token bucket for bursts)
    options.AddPolicy("search", context =>
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        
        return RateLimitPartition.GetTokenBucketLimiter(userId, key => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 50,
            TokensPerPeriod = 10,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            QueueLimit = 5
        });
    });
    
    // Policy for file uploads (concurrency limit)
    options.AddPolicy("upload", context =>
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        
        return RateLimitPartition.GetConcurrencyLimiter(userId, key => new ConcurrencyLimiterOptions
        {
            PermitLimit = 2, // Max 2 concurrent uploads per user
            QueueLimit = 1
        });
    });
    
    // Custom rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
        }
        
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests",
            message = "Rate limit exceeded. Please try again later.",
            retryAfter = retryAfter?.TotalSeconds
        }, cancellationToken);
    };
});

// Add middleware
app.UseRateLimiter();
```

### Apply to Endpoints

**Update endpoint mappings:**

```csharp
// MapAuthentication.cs
public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
{
    var auth = app.MapGroup("/api/auth")
        .WithTags("Authentication")
        .RequireRateLimiting("auth"); // Stricter rate limit
    
    auth.MapPost("/login", async (LoginRequest request, IMediator mediator) =>
    {
        // Login logic
    });
    
    auth.MapPost("/register", async (RegisterRequest request, IMediator mediator) =>
    {
        // Register logic
    });
}

// MapSearch.cs
public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
{
    var search = app.MapGroup("/api/search")
        .WithTags("Search")
        .RequireAuthorization()
        .RequireRateLimiting("search"); // Token bucket for bursts
    
    search.MapGet("/", async ([AsParameters] SearchParameters params, IMediator mediator) =>
    {
        // Search logic
    });
}

// MapImage.cs
public static void MapImageEndpoints(this IEndpointRouteBuilder app)
{
    var images = app.MapGroup("/api/images")
        .WithTags("Images")
        .RequireAuthorization();
    
    images.MapPost("/", async (IFormFile file, IMediator mediator) =>
    {
        // Upload logic
    })
    .RequireRateLimiting("upload") // Concurrency limit
    .DisableAntiforgery(); // For file uploads
}
```

### Benefits Over Custom Implementation

- ✅ **Better performance:** Lock-free concurrent implementation
- ✅ **More algorithms:** Fixed, sliding, token bucket, concurrency
- ✅ **Built-in metrics:** Automatic telemetry and monitoring
- ✅ **Standardized:** Consistent with other .NET apps
- ✅ **Less code:** No custom filter maintenance

---

## 5. Keyed Dependency Injection

### What It Is
.NET 10 enhances keyed services (introduced in .NET 8) with better performance and more intuitive APIs for registering multiple implementations of the same interface.

### The Problem

**Current approach:** Interface segregation or factory pattern

```csharp
// Multiple encryption strategies - need different interfaces
public interface IAesEncryptionService { }
public interface IChaChaEncryptionService { }

// Or use factory pattern
public interface IEncryptionServiceFactory
{
    IEncryptionService GetService(string algorithm);
}
```

### Keyed Services Solution

**Register multiple implementations:**

```csharp
// Program.cs
builder.Services.AddKeyedScoped<IEncryptionService, AesGcmEncryptionService>("aes-gcm");
builder.Services.AddKeyedScoped<IEncryptionService, ChaCha20EncryptionService>("chacha20");

// Register a default
builder.Services.AddScoped<IEncryptionService>(sp => 
    sp.GetRequiredKeyedService<IEncryptionService>("aes-gcm"));
```

**Inject by key:**

```csharp
public class ImageHandler
{
    private readonly IEncryptionService _aesService;
    private readonly IEncryptionService _chachaService;
    
    public ImageHandler(
        [FromKeyedServices("aes-gcm")] IEncryptionService aesService,
        [FromKeyedServices("chacha20")] IEncryptionService chachaService)
    {
        _aesService = aesService;
        _chachaService = chachaService;
    }
    
    public async Task<EncryptedImage> EncryptAsync(Stream image, string algorithm)
    {
        var service = algorithm switch
        {
            "AES-256-GCM" => _aesService,
            "ChaCha20-Poly1305" => _chachaService,
            _ => _aesService
        };
        
        return await service.EncryptAsync(image);
    }
}
```

### Use Cases in Storage Labels

#### 1. Multiple Database Contexts

```csharp
// Different contexts for read/write separation
builder.Services.AddKeyedDbContext<StorageLabelsDbContext>("write", 
    (sp, options) => options.UseNpgsql(writeConnectionString));

builder.Services.AddKeyedDbContext<StorageLabelsDbContext>("read", 
    (sp, options) => options.UseNpgsql(readConnectionString)
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

// In handler
public class GetBoxesHandler
{
    public GetBoxesHandler(
        [FromKeyedServices("read")] StorageLabelsDbContext readDb)
    {
        // Use read-only replica for queries
    }
}
```

#### 2. Cache Strategies

```csharp
builder.Services.AddKeyedSingleton<IDistributedCache, MemoryDistributedCache>("memory");
builder.Services.AddKeyedSingleton<IDistributedCache, RedisCache>("redis");

public class CachedSearchService
{
    private readonly IDistributedCache _l1Cache; // Memory
    private readonly IDistributedCache _l2Cache; // Redis
    
    public CachedSearchService(
        [FromKeyedServices("memory")] IDistributedCache memoryCache,
        [FromKeyedServices("redis")] IDistributedCache redisCache)
    {
        _l1Cache = memoryCache;
        _l2Cache = redisCache;
    }
    
    public async Task<T?> GetAsync<T>(string key)
    {
        // Try L1 (memory) first
        var value = await _l1Cache.GetAsync(key);
        if (value != null) return Deserialize<T>(value);
        
        // Try L2 (Redis)
        value = await _l2Cache.GetAsync(key);
        if (value != null)
        {
            // Populate L1
            await _l1Cache.SetAsync(key, value);
            return Deserialize<T>(value);
        }
        
        return default;
    }
}
```

#### 3. Authentication Providers

```csharp
builder.Services.AddKeyedScoped<IAuthenticationService, LocalAuthService>("local");
builder.Services.AddKeyedScoped<IAuthenticationService, Auth0Service>("auth0");
builder.Services.AddKeyedScoped<IAuthenticationService, AzureAdService>("azure-ad");

public class AuthenticationHandler
{
    public AuthenticationHandler(
        [FromKeyedServices("local")] IAuthenticationService localAuth,
        IConfiguration config)
    {
        // Use appropriate provider based on config
    }
}
```

### Benefits

- ✅ Single interface, multiple implementations
- ✅ No factory pattern boilerplate
- ✅ Type-safe with compile-time checking
- ✅ Better for testing (easy to mock specific keys)
- ✅ Clear intent in constructor

---

## Implementation Checklist

### Native AOT (Optional - for serverless/containers)
- [ ] Add `<PublishAot>true</PublishAot>` to csproj
- [ ] Create JSON source generation context
- [ ] Generate EF Core compiled model
- [ ] Fix any IL analyzer warnings
- [ ] Test native executable
- [ ] Update Dockerfile

### Minimal API Analyzers (Recommended)
- [ ] Add explicit route constraints (`:guid`, `:long`)
- [ ] Add `[FromRoute]`, `[FromQuery]`, `[FromServices]` attributes
- [ ] Use `Results<T1, T2, T3>` for return types
- [ ] Fix any new analyzer warnings
- [ ] Test all endpoints

### OpenAPI Generation (Recommended)
- [ ] Enable XML documentation generation
- [ ] Add XML comments to endpoints
- [ ] Create Result\<T\> transformer
- [ ] Add example responses
- [ ] Test Swagger UI

### Rate Limiting (High Priority)
- [ ] Remove custom `RateLimitFilter`
- [ ] Add built-in rate limiting service
- [ ] Configure policies (auth, search, upload)
- [ ] Apply to endpoint groups
- [ ] Test rate limit responses
- [ ] Monitor metrics

### Keyed Services (Optional - if needed)
- [ ] Identify multiple-implementation scenarios
- [ ] Register keyed services
- [ ] Update constructors with `[FromKeyedServices]`
- [ ] Test dependency resolution

---

## References

- [Native AOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot)
- [Minimal APIs Overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [OpenAPI in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi)
- [Rate Limiting Middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [Keyed Services](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#keyed-services)

---

*Document created: February 7, 2026*
*For: Storage Labels API .NET 10 Upgrade*
