# Observability & Diagnostics in .NET 10

## Overview
This guide covers .NET 10's enhanced observability features including improved metrics, activity/tracing enhancements, and TimeProvider improvements for better testing and monitoring.

---

## 1. Improved Metrics with System.Diagnostics.Metrics

### What It Is
.NET 10 enhances the metrics API with better performance, automatic collection, and standardized metric names following OpenTelemetry conventions.

### Why Metrics Matter

**Without metrics, you're flying blind:**
- How many requests per second?
- What's the P99 latency?
- Which endpoints are slowest?
- How many database queries?
- Memory/CPU usage patterns?

**With metrics:**
- ✅ Real-time performance visibility
- ✅ Identify bottlenecks quickly
- ✅ Capacity planning
- ✅ Alert on anomalies
- ✅ Track improvements

### Built-in ASP.NET Core 10 Metrics

**Automatically collected:**

| Metric | Type | Description |
|--------|------|-------------|
| `http.server.request.duration` | Histogram | Request duration in seconds |
| `http.server.active_requests` | UpDownCounter | Current active requests |
| `http.server.request.body.size` | Histogram | Request body size |
| `http.server.response.body.size` | Histogram | Response body size |
| `aspnetcore.routing.match_attempts` | Counter | Route matching attempts |
| `aspnetcore.diagnostics.exceptions` | Counter | Unhandled exceptions |
| `aspnetcore.rate_limiting.queued_requests` | Counter | Queued requests |
| `aspnetcore.rate_limiting.request.lease.duration` | Histogram | Time waiting for rate limit |

### Custom Metrics for Storage Labels

#### Step 1: Create Meter

```csharp
public class StorageLabelsMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _boxesCreated;
    private readonly Counter<long> _itemsCreated;
    private readonly Counter<long> _imageEncryptions;
    private readonly Counter<long> _imageDecryptions;
    private readonly Histogram<double> _encryptionDuration;
    private readonly Histogram<double> _searchDuration;
    private readonly Histogram<long> _imageSizes;
    private readonly UpDownCounter<int> _activeEncryptions;
    
    public StorageLabelsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("StorageLabels.API");
        
        // Counters - always increasing
        _boxesCreated = _meter.CreateCounter<long>(
            "storage.boxes.created",
            unit: "{box}",
            description: "Number of boxes created");
        
        _itemsCreated = _meter.CreateCounter<long>(
            "storage.items.created",
            unit: "{item}",
            description: "Number of items created");
        
        _imageEncryptions = _meter.CreateCounter<long>(
            "storage.images.encrypted",
            unit: "{image}",
            description: "Number of images encrypted");
        
        _imageDecryptions = _meter.CreateCounter<long>(
            "storage.images.decrypted",
            unit: "{image}",
            description: "Number of images decrypted");
        
        // Histograms - distribution of values
        _encryptionDuration = _meter.CreateHistogram<double>(
            "storage.encryption.duration",
            unit: "ms",
            description: "Image encryption duration in milliseconds");
        
        _searchDuration = _meter.CreateHistogram<double>(
            "storage.search.duration",
            unit: "ms",
            description: "Search operation duration in milliseconds");
        
        _imageSizes = _meter.CreateHistogram<long>(
            "storage.images.size",
            unit: "By",
            description: "Image file size in bytes");
        
        // UpDownCounter - can increase or decrease
        _activeEncryptions = _meter.CreateUpDownCounter<int>(
            "storage.encryption.active",
            unit: "{operation}",
            description: "Currently active encryption operations");
    }
    
    // Usage methods
    public void RecordBoxCreated(string userId, long locationId)
    {
        _boxesCreated.Add(1, 
            new KeyValuePair<string, object?>("user.id", userId),
            new KeyValuePair<string, object?>("location.id", locationId));
    }
    
    public void RecordItemCreated(string userId, Guid boxId)
    {
        _itemsCreated.Add(1,
            new KeyValuePair<string, object?>("user.id", userId),
            new KeyValuePair<string, object?>("box.id", boxId));
    }
    
    public IDisposable MeasureEncryption(string algorithm, int keyId)
    {
        _activeEncryptions.Add(1,
            new KeyValuePair<string, object?>("algorithm", algorithm));
        
        var stopwatch = Stopwatch.StartNew();
        
        return new DisposableAction(() =>
        {
            stopwatch.Stop();
            _activeEncryptions.Add(-1,
                new KeyValuePair<string, object?>("algorithm", algorithm));
            
            _encryptionDuration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("algorithm", algorithm),
                new KeyValuePair<string, object?>("key.id", keyId));
            
            _imageEncryptions.Add(1,
                new KeyValuePair<string, object?>("algorithm", algorithm));
        });
    }
    
    public void RecordImageSize(long sizeBytes, string contentType)
    {
        _imageSizes.Record(sizeBytes,
            new KeyValuePair<string, object?>("content.type", contentType));
    }
    
    public void RecordSearchDuration(long durationMs, string searchType, int resultCount)
    {
        _searchDuration.Record(durationMs,
            new KeyValuePair<string, object?>("search.type", searchType),
            new KeyValuePair<string, object?>("result.count", resultCount));
    }
}

// Helper for disposable actions
internal class DisposableAction : IDisposable
{
    private readonly Action _action;
    public DisposableAction(Action action) => _action = action;
    public void Dispose() => _action();
}
```

#### Step 2: Register Metrics

**Program.cs:**
```csharp
// Add metrics
builder.Services.AddMetrics();
builder.Services.AddSingleton<StorageLabelsMetrics>();

// Configure OpenTelemetry (optional - for export)
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("StorageLabels.API")
            .AddMeter("Microsoft.EntityFrameworkCore") // EF Core metrics
            .AddPrometheusExporter(); // Export to Prometheus
    });

// Expose metrics endpoint
app.MapPrometheusScrapingEndpoint(); // /metrics
```

#### Step 3: Use in Handlers

```csharp
public class CreateBoxHandler : IRequestHandler<CreateBoxCommand, Result<BoxResponse>>
{
    private readonly StorageLabelsMetrics _metrics;
    
    public async ValueTask<Result<BoxResponse>> Handle(
        CreateBoxCommand request,
        CancellationToken cancellationToken)
    {
        // Create box logic...
        
        // Record metric
        _metrics.RecordBoxCreated(request.UserId, box.LocationId);
        
        return Result<BoxResponse>.Success(response);
    }
}

public class EncryptImageHandler
{
    public async Task<EncryptedImage> HandleAsync(Stream image, string algorithm)
    {
        // Measure encryption operation
        using var measurement = _metrics.MeasureEncryption(algorithm, keyId);
        
        var encrypted = await _encryptionService.EncryptAsync(image);
        
        _metrics.RecordImageSize(encrypted.Length, contentType);
        
        return encrypted;
        // Metric automatically recorded when disposed
    }
}

public class SearchHandler
{
    public async ValueTask<Result<SearchResultsResponse>> Handle(
        SearchBoxesAndItemsQuery request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var results = await PerformSearchAsync(request);
        
        stopwatch.Stop();
        _metrics.RecordSearchDuration(
            stopwatch.ElapsedMilliseconds,
            "boxes_and_items",
            results.Count);
        
        return Result.Success(results);
    }
}
```

### Viewing Metrics

**Development - Console:**
```bash
dotnet-counters monitor --process-id <PID> --counters StorageLabels.API
```

**Production - Prometheus + Grafana:**
```yaml
# docker-compose.yml
services:
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"
  
  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
```

**Prometheus config:**
```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'storage-labels-api'
    scrape_interval: 5s
    static_configs:
      - targets: ['storage-labels-api:5000']
```

**Example Grafana dashboard:**
```json
{
  "panels": [
    {
      "title": "Request Rate",
      "targets": [
        {
          "expr": "rate(http_server_request_duration_count[5m])"
        }
      ]
    },
    {
      "title": "P99 Latency",
      "targets": [
        {
          "expr": "histogram_quantile(0.99, rate(http_server_request_duration_bucket[5m]))"
        }
      ]
    },
    {
      "title": "Active Encryptions",
      "targets": [
        {
          "expr": "storage_encryption_active"
        }
      ]
    },
    {
      "title": "Boxes Created per Hour",
      "targets": [
        {
          "expr": "rate(storage_boxes_created[1h]) * 3600"
        }
      ]
    }
  ]
}
```

---

## 2. Activity and Distributed Tracing

### What It Is
Activities (spans) represent units of work with timing, metadata, and parent-child relationships for distributed tracing.

### Why Tracing Matters

**See the full request flow:**
```
HTTP Request → Authentication → Database Query → Encryption → Response
     ↓              ↓                ↓              ↓           ↓
   50ms          10ms            150ms           200ms       5ms
   
Total: 415ms (where's the time spent?)
```

### Built-in Tracing

**ASP.NET Core 10 automatically creates activities for:**
- HTTP requests
- Database queries (EF Core)
- HTTP client calls
- Middleware execution

### Custom Activities

```csharp
public class ImageEncryptionService
{
    private static readonly ActivitySource _activitySource = new("StorageLabels.Encryption");
    
    public async Task<EncryptionResult> EncryptAsync(
        Stream inputStream,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("EncryptImage", ActivityKind.Internal);
        
        // Add tags for filtering/searching
        activity?.SetTag("encryption.algorithm", "AES-256-GCM");
        activity?.SetTag("encryption.key_id", activeKey.Kid);
        
        try
        {
            // Read stream
            using var readActivity = _activitySource.StartActivity("ReadImageStream");
            var plaintext = await ReadStreamAsync(inputStream);
            readActivity?.SetTag("image.size", plaintext.Length);
            
            // Encrypt
            using var cryptoActivity = _activitySource.StartActivity("AesGcmEncrypt");
            var result = PerformEncryption(plaintext);
            cryptoActivity?.SetTag("encryption.iv_size", result.InitializationVector.Length);
            
            activity?.SetTag("encryption.success", true);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### Trace Entire Request

```csharp
public class SearchBoxesAndItemsHandler
{
    private static readonly ActivitySource _activitySource = new("StorageLabels.Search");
    
    public async ValueTask<Result<SearchResultsResponse>> Handle(
        SearchBoxesAndItemsQuery request,
        CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity("SearchBoxesAndItems");
        activity?.SetTag("search.query", request.Query);
        activity?.SetTag("search.user_id", request.UserId);
        
        // Get accessible locations
        using (var authActivity = _activitySource.StartActivity("GetAccessibleLocations"))
        {
            var locations = await GetAccessibleLocationsAsync(request.UserId);
            authActivity?.SetTag("locations.count", locations.Count);
        }
        
        // Search boxes
        List<SearchResultResponse> boxResults;
        using (var boxActivity = _activitySource.StartActivity("SearchBoxes"))
        {
            boxResults = await SearchBoxesAsync(request);
            boxActivity?.SetTag("results.count", boxResults.Count);
        }
        
        // Search items
        List<SearchResultResponse> itemResults;
        using (var itemActivity = _activitySource.StartActivity("SearchItems"))
        {
            itemResults = await SearchItemsAsync(request);
            itemActivity?.SetTag("results.count", itemResults.Count);
        }
        
        var totalResults = boxResults.Count + itemResults.Count;
        activity?.SetTag("total_results", totalResults);
        
        return Result.Success(new SearchResultsResponse 
        { 
            Results = boxResults.Concat(itemResults).ToList() 
        });
    }
}
```

### Configure Tracing

**Program.cs:**
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress);
                    activity.SetTag("http.user_id", request.HttpContext.User.FindFirst("sub")?.Value);
                };
            })
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true; // Include SQL
            })
            .AddHttpClientInstrumentation()
            .AddSource("StorageLabels.*") // All custom sources
            .AddJaegerExporter(options =>
            {
                options.AgentHost = "jaeger";
                options.AgentPort = 6831;
            });
    });
```

### Viewing Traces

**Jaeger UI:**
```
http://localhost:16686

Search for:
- Service: storage-labels-api
- Operation: SearchBoxesAndItems
- Tags: error=true, search.query=*

See waterfall view of all operations with timing!
```

---

## 3. Enhanced TimeProvider

### What It Is
`TimeProvider` is an abstraction over `DateTime.UtcNow` and `Task.Delay` that makes time-dependent code testable.

### The Problem

**Hard-to-test code:**
```csharp
public class RefreshTokenService
{
    public RefreshToken CreateToken(string userId)
    {
        return new RefreshToken
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow, // Hard to test!
            ExpiresAt = DateTime.UtcNow.AddDays(7) // What time is "now"?
        };
    }
    
    public bool IsExpired(RefreshToken token)
    {
        return token.ExpiresAt < DateTime.UtcNow; // Can't control time in tests!
    }
}
```

### TimeProvider Solution

**Production code:**
```csharp
public class RefreshTokenService
{
    private readonly TimeProvider _timeProvider;
    
    public RefreshTokenService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public RefreshToken CreateToken(string userId, TimeSpan lifetime)
    {
        var now = _timeProvider.GetUtcNow();
        
        return new RefreshToken
        {
            UserId = userId,
            CreatedAt = now.DateTime,
            ExpiresAt = now.Add(lifetime).DateTime
        };
    }
    
    public bool IsExpired(RefreshToken token)
    {
        return token.ExpiresAt < _timeProvider.GetUtcNow().DateTime;
    }
    
    public async Task<CleanupResult> CleanupExpiredTokensAsync()
    {
        var deleted = await _dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAt < _timeProvider.GetUtcNow().DateTime)
            .ExecuteDeleteAsync();
        
        return new CleanupResult { DeletedCount = deleted };
    }
}
```

**Test code:**
```csharp
public class RefreshTokenServiceTests
{
    [Fact]
    public void CreateToken_SetsCorrectExpiration()
    {
        // Arrange - use fake time provider
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(new DateTime(2026, 2, 7, 12, 0, 0, DateTimeKind.Utc));
        
        var service = new RefreshTokenService(fakeTime);
        
        // Act
        var token = service.CreateToken("user123", TimeSpan.FromDays(7));
        
        // Assert
        Assert.Equal(new DateTime(2026, 2, 7, 12, 0, 0), token.CreatedAt);
        Assert.Equal(new DateTime(2026, 2, 14, 12, 0, 0), token.ExpiresAt);
    }
    
    [Fact]
    public void IsExpired_ReturnsTrueForExpiredToken()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(new DateTime(2026, 2, 7, 12, 0, 0, DateTimeKind.Utc));
        
        var service = new RefreshTokenService(fakeTime);
        var token = new RefreshToken
        {
            ExpiresAt = new DateTime(2026, 2, 6, 12, 0, 0) // Yesterday
        };
        
        // Act
        var expired = service.IsExpired(token);
        
        // Assert
        Assert.True(expired);
    }
    
    [Fact]
    public async Task CleanupExpiredTokens_AdvancesTime()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(new DateTime(2026, 2, 1));
        
        var service = new RefreshTokenService(fakeTime);
        
        // Create tokens
        var token1 = service.CreateToken("user1", TimeSpan.FromDays(1));
        var token2 = service.CreateToken("user2", TimeSpan.FromDays(10));
        
        await SaveTokensAsync(token1, token2);
        
        // Act - advance time 5 days
        fakeTime.Advance(TimeSpan.FromDays(5));
        
        var result = await service.CleanupExpiredTokensAsync();
        
        // Assert
        Assert.Equal(1, result.DeletedCount); // token1 expired, token2 still valid
    }
}
```

### FakeTimeProvider (.NET 10)

```csharp
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UtcNow;
    
    public override DateTimeOffset GetUtcNow() => _now;
    
    public void SetUtcNow(DateTime dateTime)
    {
        _now = new DateTimeOffset(dateTime, TimeSpan.Zero);
    }
    
    public void Advance(TimeSpan amount)
    {
        _now += amount;
    }
    
    public void SetLocalTimeZone(TimeZoneInfo timeZone)
    {
        LocalTimeZone = timeZone;
    }
    
    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period)
    {
        // Return controllable fake timer
        return new FakeTimer(callback, state, dueTime, period, this);
    }
}

public class FakeTimer : ITimer
{
    private readonly TimerCallback _callback;
    private readonly object? _state;
    private readonly FakeTimeProvider _timeProvider;
    
    public void Dispose() { }
    
    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        // Manual trigger
        return true;
    }
    
    public void Trigger()
    {
        _callback(_state);
    }
}
```

### Register in DI

**Program.cs:**
```csharp
// Production
builder.Services.AddSingleton(TimeProvider.System);

// Testing
services.AddSingleton<TimeProvider>(new FakeTimeProvider());
```

### More Use Cases

#### 1. Encryption Key Expiration

```csharp
public class EncryptionKeyService
{
    public async Task<List<EncryptionKey>> GetExpiredKeysAsync()
    {
        var cutoff = _timeProvider.GetUtcNow().AddDays(-90);
        
        return await _dbContext.EncryptionKeys
            .Where(k => k.CreatedAt < cutoff.DateTime)
            .ToListAsync();
    }
}
```

#### 2. Rate Limiting

```csharp
public class RateLimiter
{
    public bool IsAllowed(string userId)
    {
        var window = _timeProvider.GetUtcNow().AddMinutes(-1);
        var requests = GetRequestsSince(userId, window);
        
        return requests < MaxRequestsPerMinute;
    }
}
```

#### 3. Scheduled Tasks

```csharp
public class ScheduledCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = _timeProvider.CreateTimer(
            callback: async _ => await CleanupAsync(),
            state: null,
            dueTime: TimeSpan.FromHours(1),
            period: TimeSpan.FromHours(24));
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```

---

## Implementation Checklist

### Metrics (High Priority)
- [ ] Add StorageLabelsMetrics class
- [ ] Register metrics in DI
- [ ] Add metrics to key handlers (Create, Search, Encrypt)
- [ ] Configure OpenTelemetry exporter
- [ ] Set up Prometheus + Grafana
- [ ] Create dashboards
- [ ] Add alerts

### Tracing (Medium Priority)
- [ ] Create ActivitySource instances
- [ ] Add activities to complex operations
- [ ] Configure OpenTelemetry tracing
- [ ] Set up Jaeger
- [ ] Add correlation IDs to logs

### TimeProvider (Recommended)
- [ ] Already using TimeProvider ✓
- [ ] Use FakeTimeProvider in tests
- [ ] Test time-dependent code
- [ ] Remove any `DateTime.UtcNow` usage

---

## References

- [Metrics in .NET](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics)
- [Distributed Tracing](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing)
- [TimeProvider](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)

---

*Document created: February 7, 2026*
*For: Storage Labels API .NET 10 Upgrade*
