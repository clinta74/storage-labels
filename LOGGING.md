# Logging Guidelines

## Overview

This project uses compile-time [LoggerMessage Source Generators](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator) for structured, high-performance logging. All logging is centralized in the `LogMessages` partial class, split across domain-specific files.

## EventId Ranges

EventIds are allocated in ranges of 1000 per domain to allow for growth:

| Domain | EventId Range | File | Status |
|--------|---------------|------|--------|
| Image | 1000-1999 | `LogMessages.Image.cs` | ✅ Migrated |
| Item | 2000-2999 | `LogMessages.Item.cs` | ✅ Migrated |
| Authentication | 3000-3999 | `LogMessages.Authentication.cs` | ✅ Migrated |
| User | 4000-4999 | `LogMessages.User.cs` | ✅ Migrated |
| EncryptionKey | 5000-5999 | `LogMessages.EncryptionKey.cs` | ✅ Migrated |
| Box | 6000-6999 | `LogMessages.Box.cs` | ✅ Migrated |
| Location | 7000-7999 | `LogMessages.Location.cs` | ✅ Migrated |
| Initialization | 8000-8999 | `LogMessages.Initialization.cs` | ✅ Migrated |
| CommonLocation | 10000-10999 | `LogMessages.CommonLocation.cs` | ✅ Migrated |
| Authorization | 11000-11999 | `LogMessages.Authorization.cs` | ✅ Migrated |
| Search | 12000-12999 | `LogMessages.Search.cs` | ✅ Migrated |
| _Reserved_ | 13000+ | — | Available for future domains |

### EventId Migration History

**Breaking Changes (No monitoring impact confirmed):**
- Image: EventIds 1-12 → 1000-1011 (February 2026)
- Item: EventId 13 → 2000 (February 2026)

## LoggerMessage Pattern

### Attribute Format

All LoggerMessage methods use **multi-line attribute format** for consistency and readability:

```csharp
[LoggerMessage(
    EventId = 1000,
    Level = LogLevel.Information,
    Message = "Image {ImageId} uploaded by user {UserId} with filename {FileName}")]
public static partial void LogImageUploaded(
    this ILogger logger,
    Guid imageId,
    string userId,
    string fileName);
```

### With Exception Parameter

For error logging with exceptions, add the exception parameter **before the cancellation token** (if any):

```csharp
[LoggerMessage(
    EventId = 1015,
    Level = LogLevel.Error,
    Message = "Failed to decrypt image {ImageId}")]
public static partial void ImageDecryptionFailed(
    this ILogger logger,
    Exception ex,
    Guid imageId,
    string userId);
```

### File Structure

Each domain has its own partial class file:

```csharp
using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    // Image-related logging methods (EventId 1000-1999)
    
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "...")]
    public static partial void SomeMethod(this ILogger logger, ...);
}
```

## Naming Conventions

### Method Names
- Use **past-tense, descriptive names**: `ImageUploaded`, `UserRegistered`, `KeyRotationStarted`
- Prefix with domain context when needed: `LogImageUploaded`, `EncryptionKeyCreated`
- Be specific about the action: `RefreshTokenRevoked` (not just `TokenRevoked`)

### Parameters
- Use **camelCase** for all parameters: `imageId`, `userId`, `username`
- Match parameter names to the most common usage in handlers/services
- Use full names over abbreviations: `encryptionKeyId` (not `keyId` or `kid`)
- Be consistent: always use `userId` (not `user` or `UserId`)

### Message Templates
- Use structured logging placeholders: `{ImageId}`, `{UserId}`, `{FileName}`
- Keep messages concise but informative
- Include key identifiers for traceability
- Use consistent formatting: "Image {ImageId} uploaded by user {UserId}"

## Adding a New Domain

When adding a new logging domain:

1. **Assign EventId Range**: Choose the next available 1000-range (e.g., 13000-13999)
2. **Create LogMessages File**: `LogMessages.YourDomain.cs` in `storage-labels-api/Logging/`
3. **Use Partial Class Pattern**:
   ```csharp
   using Microsoft.Extensions.Logging;
   
   namespace StorageLabelsApi.Logging;
   
   public static partial class LogMessages
   {
       // YourDomain methods starting at assigned EventId
   }
   ```
4. **Update LOGGING.md**: Add the new domain to the EventId Ranges table
5. **Follow Conventions**: Multi-line format, camelCase parameters, past-tense method names

## Usage in Code

### ✅ Correct (Use LoggerMessage)

```csharp
_logger.LogImageUploaded(imageId, userId, fileName);
_logger.ImageEncryptionFailed(ex, imageId);
```

### ❌ Incorrect (Direct logging methods)

```csharp
// DO NOT USE - these bypass compile-time optimization
_logger.LogInformation("Image {ImageId} uploaded", imageId);
_logger.LogWarning("No active encryption key found");
_logger.LogError(ex, "Failed to encrypt image {ImageId}", imageId);
```

## Benefits

- **Performance**: Compile-time code generation eliminates runtime overhead
- **Type Safety**: Parameter mismatches caught at compile time
- **Consistency**: Centralized logging messages prevent duplication
- **Traceability**: EventIds enable filtering and monitoring
- **Maintainability**: All logging in one location makes updates easier

## Testing

When writing tests that assert on logging:

```csharp
// Assert on EventId
mockLogger.Verify(
    x => x.Log(
        LogLevel.Information,
        It.Is<EventId>(e => e.Id == 1000),
        It.IsAny<It.IsAnyType>(),
        null,
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
    Times.Once);

// Or use test helpers that check for specific LoggerMessage calls
```

Update tests when EventIds change during migrations.

## References

- [High-performance logging with LoggerMessage in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
- [Logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [LoggerMessage attribute](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loggermessageattribute)
