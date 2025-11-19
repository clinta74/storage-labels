using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.Settings;
using StorageLabelsApi.Services;
using StorageLabelsApi.Logging;
using Microsoft.Extensions.Options;

namespace StorageLabelsApi.Handlers.Initialization;

/// <summary>
/// Request to initialize database with required startup data
/// </summary>
public record InitializeDatabaseRequest : IRequest<Result>;

/// <summary>
/// Handles database initialization including migrations, roles, and required user records
/// </summary>
public class InitializeDatabaseHandler : IRequestHandler<InitializeDatabaseRequest, Result>
{
    private readonly StorageLabelsDbContext _context;
    private readonly RoleInitializationService? _roleInitService;
    private readonly AuthenticationSettings _authSettings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InitializeDatabaseHandler> _logger;

    public InitializeDatabaseHandler(
        StorageLabelsDbContext context,
        IOptions<AuthenticationSettings> authSettings,
        TimeProvider timeProvider,
        ILogger<InitializeDatabaseHandler> logger,
        RoleInitializationService? roleInitService = null)
    {
        _context = context;
        _authSettings = authSettings.Value;
        _timeProvider = timeProvider;
        _logger = logger;
        _roleInitService = roleInitService;
    }

    public async ValueTask<Result> Handle(InitializeDatabaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Apply pending migrations
            await _context.Database.MigrateAsync(cancellationToken);
            _logger.DatabaseMigrationsApplied();

            if (_authSettings.Mode == AuthenticationMode.Local)
            {
                await InitializeLocalModeAsync(cancellationToken);
            }
            else if (_authSettings.Mode == AuthenticationMode.None)
            {
                await InitializeNoAuthModeAsync(cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.DatabaseInitializationFailed(ex);
            return Result.Error("Database initialization failed");
        }
    }

    private async Task InitializeLocalModeAsync(CancellationToken cancellationToken)
    {
        if (_roleInitService == null)
        {
            _logger.RoleServiceNotAvailable();
            return;
        }

        // Initialize roles (Admin, Auditor, User)
        await _roleInitService.InitializeRolesAsync();
        _logger.RolesInitialized();
        
        // Note: First registered user automatically becomes Admin (see LocalAuthenticationService.RegisterAsync)
        _logger.FirstUserWillBeAdmin();
    }

    private async Task InitializeNoAuthModeAsync(CancellationToken cancellationToken)
    {
        // In NoAuth mode, ensure the "anonymous" user exists in the database
        // This is required for foreign key constraints (UserLocation, ImageMetadata, etc.)
        const string AnonymousUserId = "00000000-0000-0000-0000-000000000001";
        var existingUser = await _context.Users.FindAsync(new object[] { AnonymousUserId }, cancellationToken);

        if (existingUser == null)
        {
            var anonymousUser = new User(
                UserId: AnonymousUserId,
                FirstName: "Anonymous",
                LastName: "User",
                EmailAddress: "anonymous@localhost",
                Created: _timeProvider.GetUtcNow()
            );

            _context.Users.Add(anonymousUser);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.AnonymousUserCreated();
        }
        else
        {
            _logger.AnonymousUserExists();
        }
    }
}
