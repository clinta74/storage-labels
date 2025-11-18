using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Services;

/// <summary>
/// Background service for rotating encryption keys
/// </summary>
public class KeyRotationService : IKeyRotationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KeyRotationService> _logger;
    private readonly IRotationProgressNotifier _progressNotifier;
    private readonly TimeProvider _timeProvider;

    public KeyRotationService(
        IServiceScopeFactory scopeFactory,
        ILogger<KeyRotationService> logger,
        IRotationProgressNotifier progressNotifier,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _progressNotifier = progressNotifier;
        _timeProvider = timeProvider;
    }

    public async Task<EncryptionKeyRotation> StartRotationAsync(
        RotationOptions options,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>();

        // Special case: fromKeyId = null or 0 means encrypting unencrypted images
        if (!options.FromKeyId.HasValue || options.FromKeyId == 0)
        {
            var toKey = await context.EncryptionKeys.FindAsync([options.ToKeyId], cancellationToken);
            if (toKey == null)
                throw new InvalidOperationException($"Target key {options.ToKeyId} not found");

            if (toKey.Status != EncryptionKeyStatus.Active)
                throw new InvalidOperationException($"Target key {options.ToKeyId} is not active (status: {toKey.Status})");

            // Count unencrypted images
            var totalImages = await context.Images
                .Where(img => !img.IsEncrypted)
                .CountAsync(cancellationToken);

            // Create rotation record (fromKeyId=null indicates migration from unencrypted)
            var rotation = new EncryptionKeyRotation
            {
                FromKeyId = null,
                ToKeyId = options.ToKeyId,
                Status = RotationStatus.InProgress,
                TotalImages = totalImages,
                BatchSize = options.BatchSize,
                InitiatedBy = options.InitiatedBy,
                IsAutomatic = options.IsAutomatic
            };

            context.EncryptionKeyRotations.Add(rotation);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Started unencrypted image migration {RotationId}: unencrypted -> {ToKey} ({TotalImages} images, batch size {BatchSize})",
                rotation.Id,
                toKey.Kid,
                totalImages,
                options.BatchSize);

            // Start background rotation
            _ = Task.Run(() => PerformRotationAsync(rotation.Id), CancellationToken.None);

            return rotation;
        }

        // Standard rotation between two encrypted keys
        var fromKey = await context.EncryptionKeys.FindAsync([options.FromKeyId.Value], cancellationToken);
        var toKey2 = await context.EncryptionKeys.FindAsync([options.ToKeyId], cancellationToken);

        if (fromKey == null)
            throw new InvalidOperationException($"Source key {options.FromKeyId} not found");
        if (toKey2 == null)
            throw new InvalidOperationException($"Target key {options.ToKeyId} not found");

        // Count images to rotate
        var totalImages2 = await context.Images
            .Where(img => img.IsEncrypted && img.EncryptionKeyId == options.FromKeyId.Value)
            .CountAsync(cancellationToken);

        // Create rotation record
        var rotation2 = new EncryptionKeyRotation
        {
            FromKeyId = options.FromKeyId,
            ToKeyId = options.ToKeyId,
            Status = RotationStatus.InProgress,
            TotalImages = totalImages2,
            BatchSize = options.BatchSize,
            InitiatedBy = options.InitiatedBy,
            IsAutomatic = options.IsAutomatic
        };

        context.EncryptionKeyRotations.Add(rotation2);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Started key rotation {RotationId}: {FromKey} -> {ToKey} ({TotalImages} images, batch size {BatchSize})",
            rotation2.Id,
            fromKey.Kid,
            toKey2.Kid,
            totalImages2,
            options.BatchSize);

        // Start background rotation
        _ = Task.Run(() => PerformRotationAsync(rotation2.Id), CancellationToken.None);

        return rotation2;
    }

    public async Task<RotationProgress?> GetRotationProgressAsync(
        Guid rotationId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>();

        var rotation = await context.EncryptionKeyRotations
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rotationId, cancellationToken);

        if (rotation == null)
            return null;

        var percentComplete = rotation.TotalImages > 0
            ? (double)rotation.ProcessedImages / rotation.TotalImages * 100
            : 0;

        return new RotationProgress(
            rotation.Id,
            rotation.Status,
            rotation.TotalImages,
            rotation.ProcessedImages,
            rotation.FailedImages,
            percentComplete,
            rotation.StartedAt,
            rotation.CompletedAt,
            rotation.ErrorMessage
        );
    }

    public async Task<List<EncryptionKeyRotation>> GetRotationsAsync(
        RotationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>();

        var query = context.EncryptionKeyRotations
            .Include(r => r.FromKey)
            .Include(r => r.ToKey)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CancelRotationAsync(
        Guid rotationId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>();

        var rotation = await context.EncryptionKeyRotations
            .FirstOrDefaultAsync(r => r.Id == rotationId, cancellationToken);

        if (rotation == null || rotation.Status != RotationStatus.InProgress)
            return false;

        rotation.Status = RotationStatus.Cancelled;
        rotation.CompletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled rotation {RotationId}", rotationId);

        return true;
    }

    private async Task PerformRotationAsync(Guid rotationId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<IImageEncryptionService>();

            var rotation = await context.EncryptionKeyRotations
                .Include(r => r.FromKey)
                .Include(r => r.ToKey)
                .FirstOrDefaultAsync(r => r.Id == rotationId);

            if (rotation == null)
            {
                _logger.LogError("Rotation {RotationId} not found", rotationId);
                return;
            }

            var isEncryptionMigration = !rotation.FromKeyId.HasValue;

            _logger.LogInformation(
                "Beginning {Operation} {RotationId}: {FromKey} -> {ToKey}",
                isEncryptionMigration ? "migration" : "rotation",
                rotationId,
                isEncryptionMigration ? "unencrypted" : rotation.FromKey!.Kid.ToString(),
                rotation.ToKey!.Kid);

            var batchNumber = 0;
            var hasMore = true;

            while (hasMore && rotation.Status == RotationStatus.InProgress)
            {
                // Refresh rotation status in case it was cancelled
                await context.Entry(rotation).ReloadAsync();

                if (rotation.Status == RotationStatus.Cancelled)
                {
                    _logger.LogInformation("{Operation} {RotationId} was cancelled", 
                        isEncryptionMigration ? "Migration" : "Rotation",
                        rotationId);
                    break;
                }

                // Get next batch of images - different query for encryption vs rotation
                var images = isEncryptionMigration
                    ? await context.Images
                        .Where(img => !img.IsEncrypted)
                        .Take(rotation.BatchSize)
                        .ToListAsync()
                    : await context.Images
                        .Where(img => img.IsEncrypted && img.EncryptionKeyId == rotation.FromKeyId!.Value)
                        .Take(rotation.BatchSize)
                        .ToListAsync();

                hasMore = images.Count == rotation.BatchSize;
                batchNumber++;

                _logger.LogInformation(
                    "Processing batch {BatchNumber} of {Operation} {RotationId}: {Count} images",
                    batchNumber,
                    isEncryptionMigration ? "migration" : "rotation",
                    rotationId,
                    images.Count);

                foreach (var image in images)
                {
                    try
                    {
                        if (isEncryptionMigration)
                        {
                            // Encrypt unencrypted image for the first time
                            await encryptionService.EncryptExistingImageAsync(
                                image,
                                rotation.ToKeyId,
                                CancellationToken.None);
                        }
                        else
                        {
                            // Re-encrypt already encrypted image
                            await encryptionService.ReEncryptImageAsync(
                                image,
                                rotation.ToKeyId,
                                CancellationToken.None);
                        }

                        rotation.ProcessedImages++;
                    }
                    catch (Exception ex)
                    {
                        rotation.FailedImages++;
                        _logger.LogError(
                            ex,
                            "Failed to {Operation} image {ImageId} during {Process} {RotationId}",
                            isEncryptionMigration ? "encrypt" : "re-encrypt",
                            image.ImageId,
                            isEncryptionMigration ? "migration" : "rotation",
                            rotationId);
                    }
                }

                await context.SaveChangesAsync();

                // Notify progress to SSE clients
                var percentComplete = rotation.TotalImages > 0
                    ? (double)rotation.ProcessedImages / rotation.TotalImages * 100
                    : 0;

                await _progressNotifier.NotifyProgressAsync(rotationId, new RotationProgress(
                    rotation.Id,
                    rotation.Status,
                    rotation.TotalImages,
                    rotation.ProcessedImages,
                    rotation.FailedImages,
                    percentComplete,
                    rotation.StartedAt,
                    rotation.CompletedAt,
                    rotation.ErrorMessage
                ));

                _logger.LogInformation(
                    "Batch {BatchNumber} complete: {Processed}/{Total} images rotated ({Failed} failed)",
                    batchNumber,
                    rotation.ProcessedImages,
                    rotation.TotalImages,
                    rotation.FailedImages);
            }

            // Mark rotation as complete
            if (rotation.Status == RotationStatus.InProgress)
            {
                rotation.Status = rotation.FailedImages > 0
                    ? RotationStatus.Failed
                    : RotationStatus.Completed;
                rotation.CompletedAt = DateTime.UtcNow;

                if (rotation.FailedImages > 0)
                {
                    rotation.ErrorMessage = $"{rotation.FailedImages} images failed to rotate";
                }

                await context.SaveChangesAsync();

                // Send final progress notification
                var percentComplete = rotation.TotalImages > 0
                    ? (double)rotation.ProcessedImages / rotation.TotalImages * 100
                    : 0;

                await _progressNotifier.NotifyProgressAsync(rotationId, new RotationProgress(
                    rotation.Id,
                    rotation.Status,
                    rotation.TotalImages,
                    rotation.ProcessedImages,
                    rotation.FailedImages,
                    percentComplete,
                    rotation.StartedAt,
                    rotation.CompletedAt,
                    rotation.ErrorMessage
                ));

                _logger.LogInformation(
                    "Rotation {RotationId} {Status}: {Processed}/{Total} images rotated, {Failed} failed",
                    rotationId,
                    rotation.Status,
                    rotation.ProcessedImages,
                    rotation.TotalImages,
                    rotation.FailedImages);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during rotation {RotationId}", rotationId);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>();

                var rotation = await context.EncryptionKeyRotations.FindAsync(rotationId);
                if (rotation != null)
                {
                    rotation.Status = RotationStatus.Failed;
                    rotation.CompletedAt = _timeProvider.GetUtcNow().DateTime;
                    rotation.ErrorMessage = ex.Message;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to update rotation {RotationId} status after error", rotationId);
            }
        }
    }
}

