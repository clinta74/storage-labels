# Plan: Simplified Self-Hosted Authentication (Local + No Auth)

This plan removes all Auth0, OAuth, and OIDC support, keeping only:
- **Local Authentication** - ASP.NET Core Identity with JWT tokens
- **No Authentication** - All users get full permissions (trusted network mode)

## Code Patterns to Follow

### Models Pattern
Use **records** with primary constructors for all DTOs and requests:
```csharp
public record LoginRequest(string UsernameOrEmail, string Password, bool RememberMe = false);
public record AuthenticationResult(string Token, DateTime ExpiresAt, UserInfoResponse User);
```

### Handler Pattern
Use **Mediator** with `IRequest<Result<T>>` and validate with **FluentValidation**:
```csharp
public record Login(string UsernameOrEmail, string Password) : IRequest<Result<AuthenticationResult>>;

public class LoginHandler(UserManager<ApplicationUser> userManager, JwtTokenService jwtService, ILogger<LoginHandler> logger) 
    : IRequestHandler<Login, Result<AuthenticationResult>>
{
    public async ValueTask<Result<AuthenticationResult>> Handle(Login request, CancellationToken cancellationToken)
    {
        var validation = await new LoginValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthenticationResult>.Invalid(validation.AsErrors());
        }
        // ... handler logic
    }
}

public class LoginValidator : AbstractValidator<Login>
{
    public LoginValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
```

### Logging Pattern
Use **LoggerMessage** source generators in partial classes:
```csharp
// LogMessages.Authentication.cs
public static partial class LogMessages
{
    [LoggerMessage(Message = "User ({username}) login failed - invalid credentials.", Level = LogLevel.Warning)]
    public static partial void LoginFailed(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) logged in successfully.", Level = LogLevel.Information)]
    public static partial void LoginSucceeded(this ILogger logger, string username);
}
```

---

## Phase 1: Simplify Authentication Settings & Models

### 1.1 Update AuthenticationSettings.cs
Remove Auth0, OIDC, and external provider settings. Keep only Local and None modes.

**File:** `storage-labels-api/Models/Settings/AuthenticationSettings.cs`

**Changes:**
- Remove `Auth0`, `OIDC` enum values (keep `None`, `Local`)
- Remove `Auth0Settings`, `OIDCSettings`, `ExternalProvidersSettings` properties
- Keep only `LocalAuthSettings`

### 1.2 Update ApplicationUser.cs
Remove external provider properties since we're not supporting OAuth.

**File:** `storage-labels-api/Datalayer/Models/ApplicationUser.cs`

**Changes:**
- Remove `ExternalProvider` property
- Remove `ExternalProviderId` property
- Keep FullName, ProfilePictureUrl, CreatedAt, UpdatedAt, IsActive

---

## Phase 2: Create Authentication Services

### 2.1 Create JWT Settings Model
**File:** `storage-labels-api/Models/Settings/JwtSettings.cs` (new)

```csharp
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "storage-labels-api";
    public string Audience { get; set; } = "storage-labels-ui";
    public int ExpirationMinutes { get; set; } = 60;
}
```

### 2.2 Create Authentication DTOs (Records)
**Files:** `storage-labels-api/Models/DTO/Authentication/` (new folder)

Use **record** types with primary constructors:

- `LoginRequest.cs`:
```csharp
public record LoginRequest(string UsernameOrEmail, string Password, bool RememberMe = false);
```

- `RegisterRequest.cs`:
```csharp
public record RegisterRequest(string Email, string Username, string Password, string? FullName = null);
```

- `AuthenticationResult.cs`:
```csharp
public record AuthenticationResult(string Token, DateTime ExpiresAt, UserInfoResponse User);
```

- `UserInfoResponse.cs`:
```csharp
public record UserInfoResponse(
    string UserId, 
    string Username, 
    string Email, 
    string? FullName, 
    string? ProfilePictureUrl, 
    string[] Roles, 
    string[] Permissions, 
    bool IsActive
);
```

- `AuthConfigResponse.cs`:
```csharp
public record AuthConfigResponse(AuthenticationMode Mode, bool AllowRegistration, bool RequireEmailConfirmation);
```

### 2.3 Create JWT Token Service
**File:** `storage-labels-api/Services/JwtTokenService.cs` (new)

Generate JWT tokens with user claims and permissions.

### 2.4 Create Authentication Service Interface
**File:** `storage-labels-api/Services/Authentication/IAuthenticationService.cs` (new)

```csharp
public interface IAuthenticationService
{
    Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthenticationResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<UserInfoResponse>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
}
```

### 2.5 Create Logging Messages for Authentication
**File:** `storage-labels-api/Logging/LogMessages.Authentication.cs` (new)

```csharp
public static partial class LogMessages
{
    [LoggerMessage(Message = "User ({username}) login attempt.", Level = LogLevel.Information)]
    public static partial void LoginAttempt(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) login failed - invalid credentials.", Level = LogLevel.Warning)]
    public static partial void LoginFailed(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) login succeeded.", Level = LogLevel.Information)]
    public static partial void LoginSucceeded(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) registration attempt.", Level = LogLevel.Information)]
    public static partial void RegistrationAttempt(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) registration succeeded.", Level = LogLevel.Information)]
    public static partial void RegistrationSucceeded(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) registration failed - {reason}.", Level = LogLevel.Warning)]
    public static partial void RegistrationFailed(this ILogger logger, string username, string reason);
    
    [LoggerMessage(Message = "User ({userId}) account is locked.", Level = LogLevel.Warning)]
    public static partial void AccountLocked(this ILogger logger, string userId);
    
    [LoggerMessage(Message = "User ({userId}) account is inactive.", Level = LogLevel.Warning)]
    public static partial void AccountInactive(this ILogger logger, string userId);
}
```

### 2.6 Implement Local Authentication Service
**File:** `storage-labels-api/Services/Authentication/LocalAuthenticationService.cs` (new)

Use `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, and `JwtTokenService`.
Include proper **logging** using LogMessages pattern.

### 2.7 Implement No Auth Service
**File:** `storage-labels-api/Services/Authentication/NoAuthenticationService.cs` (new)

Always return success, always return all permissions.

---

## Phase 3: Update Authorization

### 3.1 Create Claims Transformation
**File:** `storage-labels-api/Services/ClaimsTransformationService.cs` (new)

Transform Identity claims to include permissions based on user roles.

### 3.2 Update HasScopeHandler
**File:** `storage-labels-api/Authorization/HasScopeHandler.cs`

**Changes:**
- Support both Auth0-style "permissions" claim AND local "permission" claims
- In No Auth mode, always succeed
- Map roles to permissions

### 3.3 Create Default Roles and Permissions
**File:** `storage-labels-api/Services/RoleInitializationService.cs` (new)

Seed default roles:
- **Admin** - all permissions
- **User** - read permissions only

---

## Phase 4: Create Authentication Endpoints & Handlers

### 4.1 Create Authentication Handlers (Mediator Pattern)
**Files:** `storage-labels-api/Handlers/Authentication/` (new folder)

All handlers follow the **Mediator + FluentValidation** pattern:

- `LoginHandler.cs`:
```csharp
public record Login(string UsernameOrEmail, string Password, bool RememberMe = false) : IRequest<Result<AuthenticationResult>>;

public class LoginHandler(IAuthenticationService authService, ILogger<LoginHandler> logger) 
    : IRequestHandler<Login, Result<AuthenticationResult>>
{
    public async ValueTask<Result<AuthenticationResult>> Handle(Login request, CancellationToken cancellationToken)
    {
        var validation = await new LoginValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthenticationResult>.Invalid(validation.AsErrors());
        }

        var loginRequest = new LoginRequest(request.UsernameOrEmail, request.Password, request.RememberMe);
        return await authService.LoginAsync(loginRequest, cancellationToken);
    }
}

public class LoginValidator : AbstractValidator<Login>
{
    public LoginValidator()
    {
        RuleFor(x => x.UsernameOrEmail).NotEmpty().WithMessage("Username or email is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
    }
}
```

- `RegisterHandler.cs` - Similar pattern with validation
- `LogoutHandler.cs` - Simple handler, no validation needed
- `GetCurrentUserInfoHandler.cs` - Fetch user info + permissions
- `GetAuthConfigHandler.cs` - Return auth mode from settings

### 4.2 Create Authentication Endpoints
### 4.2 Create Authentication Endpoints
**File:** `storage-labels-api/Endpoints/MapAuthentication.cs` (new)

Endpoints:
- `POST /api/auth/login` - Login with credentials
- `POST /api/auth/register` - Register new user
- `POST /api/auth/logout` - Logout
- `GET /api/auth/me` - Get current user info + permissions
- `GET /api/auth/config` - Get auth mode (Local or None)

### 4.3 Create Validators for Each Handler
Create separate validator classes for each handler following FluentValidation pattern.

---

## Phase 5: Consolidate User Registration Flow

### 5.1 Update RegisterRequest DTO
**File:** `storage-labels-api/Models/DTO/Authentication/RegisterRequest.cs`

**Changes:**
- Add `FirstName` and `LastName` to RegisterRequest
- These will be stored in the User table during registration

```csharp
public record RegisterRequest(
    string Email, 
    string Username, 
    string Password, 
    string FirstName,
    string LastName,
    string? FullName = null
);
```

### 5.2 Update LocalAuthenticationService.RegisterAsync
**File:** `storage-labels-api/Services/Authentication/LocalAuthenticationService.cs`

**Changes:**
- After creating ApplicationUser, also create User record in database
- Use the same pattern as CreateNewUserHandler but integrate into registration
- Set EmailAddress from ApplicationUser.Email
- Set FirstName and LastName from request

```csharp
// After successful user creation:
var user = new User(
    UserId: applicationUser.Id,
    FirstName: request.FirstName,
    LastName: request.LastName,
    EmailAddress: applicationUser.Email!,
    Created: timeProvider.GetUtcNow()
);
await dbContext.Users.AddAsync(user, cancellationToken);
await dbContext.SaveChangesAsync(cancellationToken);
```

### 5.3 Update Register Handler & Validator
**File:** `storage-labels-api/Handlers/Authentication/RegisterHandler.cs`

**Changes:**
- Add FirstName and LastName to Register record
- Add validation rules for FirstName and LastName (required, min length)

### 5.4 Remove Old New User Flow (Later Phase)
- Keep MapNewUser.cs, GetNewUserHandler.cs for now (backward compatibility)
- Mark as deprecated
- Will be removed in Phase 9 along with Auth0 dependencies

---

## Phase 6: Update Program.cs

### 6.1 Remove Auth0 Dependencies
**File:** `storage-labels-api/Program.cs`

**Changes:**
- Remove Auth0Settings configuration
- Remove Auth0 JWT Bearer configuration
- Remove IAuth0ManagementApiClient registration

### 6.2 Add Conditional Authentication
**File:** `storage-labels-api/Program.cs`

**Add:**
```csharp
var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>();

if (authSettings.Mode == AuthenticationMode.Local)
{
    // Configure ASP.NET Core Identity
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Password settings from authSettings.Local
    })
    .AddEntityFrameworkStores<StorageLabelsDbContext>()
    .AddDefaultTokenProviders();
    
    // Configure JWT authentication
    var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // JWT configuration
        });
    
    builder.Services.AddScoped<IAuthenticationService, LocalAuthenticationService>();
}
else if (authSettings.Mode == AuthenticationMode.None)
{
    // No authentication - open access
    builder.Services.AddScoped<IAuthenticationService, NoAuthenticationService>();
}
```

### 6.3 Register New Services
- `JwtTokenService`
- `ClaimsTransformationService`
- `RoleInitializationService`

---

## Phase 7: Database Migration

### 7.1 Create Migration
```bash
cd storage-labels-api
dotnet ef migrations add AddLocalAuthentication
```

This creates Identity tables if they don't exist.

### 7.2 Seed Default Admin User
**File:** `storage-labels-api/Services/DatabaseSeeder.cs` (new)

Create a default admin user on first run (configurable via environment variables).

---

## Phase 8: Update UI

### 8.1 Create Local Auth Provider
**File:** `storage-labels-ui/src/auth/local-auth-provider.tsx` (new)

Handle local login/logout, store JWT token.

### 8.2 Create Login Component
**File:** `storage-labels-ui/src/app/components/auth/login.tsx` (new)

Username/password login form.

### 8.3 Create Registration Component
**File:** `storage-labels-ui/src/app/components/auth/register.tsx` (new)

Registration form (if enabled) - includes FirstName and LastName fields.

### 8.4 Update Auth Provider
**File:** `storage-labels-ui/src/auth/auth-provider.tsx`

**Changes:**
- Fetch auth config from `/api/auth/config`
- If mode is `Local`, use LocalAuthProvider
- If mode is `None`, skip auth entirely
- Remove Auth0 provider

### 8.5 Update User Permission Provider
**File:** `storage-labels-ui/src/app/providers/user-permission-provider.tsx`

**Changes:**
- Fetch permissions from `/api/auth/me` instead of JWT decode
- Support both JWT token permissions and API-returned permissions
- In No Auth mode, return all permissions

### 8.6 Update Navigation
**File:** `storage-labels-ui/src/app/components/navigation-bar.tsx`

**Changes:**
- Add Login/Logout buttons
- Show current user info
- Handle No Auth mode (show warning banner)

---

## Phase 9: Configuration Files

### 9.1 Update appsettings.json
**File:** `storage-labels-api/appsettings.json`

```json
{
  "Authentication": {
    "Mode": "Local",
    "Local": {
      "Enabled": true,
      "AllowRegistration": true,
      "RequireEmailConfirmation": false,
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
    "Secret": "your-secret-key-min-32-chars-change-in-production",
    "Issuer": "storage-labels-api",
    "Audience": "storage-labels-ui",
    "ExpirationMinutes": 60
  }
}
```

### 8.2 Update docker-compose-custom-config.yaml
**File:** `docker-compose-custom-config.yaml`

```yaml
storage-labels-api:
  environment:
    # Authentication Mode: Local or None
    - Authentication__Mode=Local
    
    # JWT Settings (required for Local mode)
    - Jwt__Secret=${JWT_SECRET}
    - Jwt__Issuer=storage-labels-api
    - Jwt__Audience=storage-labels-ui
    - Jwt__ExpirationMinutes=60
    
    # Local Auth Settings
    - Authentication__Local__AllowRegistration=true
    - Authentication__Local__RequireEmailConfirmation=false
    
    # Default Admin (created on first run)
    - DefaultAdmin__Username=admin
    - DefaultAdmin__Password=${ADMIN_PASSWORD}
    - DefaultAdmin__Email=admin@localhost
```

### 8.3 Create .env.example
**File:** `.env.example`

```bash
# JWT Secret (generate with: openssl rand -base64 32)
JWT_SECRET=your-secure-random-key-here-min-32-characters

# Default Admin Credentials
ADMIN_PASSWORD=ChangeMe123!

# Database
POSTGRES_PASSWORD=your-db-password
```

---

## Phase 10: Remove Auth0 Dependencies

### 10.1 Remove NuGet Packages
```bash
cd storage-labels-api
dotnet remove package Auth0.AspNetCore.Authentication
dotnet remove package Auth0.ManagementApi
```

### 10.2 Delete Auth0 Files
- `storage-labels-api/Models/Settings/Auth0Settings.cs`
- `storage-labels-api/Services/Auth0ManagementApiClient.cs`
- `storage-labels-api/Services/IAuth0ManagementApiClient.cs`
- `storage-labels-api/Handlers/NewUsers/GetNewUserHandler.cs` (uses Auth0)
- `storage-labels-api/Endpoints/MapNewUser.cs` (old flow)
- `storage-labels-api/Models/DTO/NewUser/GetNewUserResponse.cs`

### 10.3 Update CreateNewUserHandler
**File:** `storage-labels-api/Handlers/Users/CreateNewUserHandler.cs`

**Changes:**
- Remove Auth0ManagementApiClient dependency
- This handler is no longer needed since User creation happens in RegisterAsync
- Can be removed or simplified for admin user creation only

### 10.4 Remove Auth0 from UI
**Files to delete:**
- `storage-labels-ui/src/auth/auth0-provider.tsx` (if exists)

**Update package.json:**
- Remove `@auth0/auth0-react` dependency

---

## Phase 11: Testing & Documentation

## Phase 11: Testing & Documentation

### 11.1 Create Tests
- Login/register flows
- JWT token generation
- Permission checking
- No Auth mode behavior

### 11.2 Update README
Document:
- How to configure Local vs No Auth
- How to create the default admin user
- How to generate JWT secret
- Security considerations

---

## Summary of Changes

### Files to Create (23 new files)
1. `JwtSettings.cs`
2. `LoginRequest.cs`, `RegisterRequest.cs`, `AuthenticationResult.cs`, `UserInfoResponse.cs`
3. `JwtTokenService.cs`
4. `IAuthenticationService.cs`
5. `LocalAuthenticationService.cs`
6. `NoAuthenticationService.cs`
7. `ClaimsTransformationService.cs`
8. `RoleInitializationService.cs`
9. `DatabaseSeeder.cs`
10. `MapAuthentication.cs`
11. `LoginHandler.cs`, `RegisterHandler.cs`, `LogoutHandler.cs`, `GetCurrentUserInfoHandler.cs`, `GetAuthConfigHandler.cs`
12. `local-auth-provider.tsx`
13. `login.tsx`, `register.tsx`
14. `.env.example`

### Files to Modify (8 files)
1. `AuthenticationSettings.cs` - Remove Auth0/OIDC/External
2. `ApplicationUser.cs` - Remove external provider fields
3. `HasScopeHandler.cs` - Support local claims
4. `Program.cs` - Remove Auth0, add conditional auth
5. `auth-provider.tsx` - Support local auth
6. `user-permission-provider.tsx` - Fetch from API
7. `navigation-bar.tsx` - Add login/logout
8. `appsettings.json`, `docker-compose-custom-config.yaml`

### Files to Delete (3-4 files)
1. `Auth0Settings.cs`
2. `Auth0ManagementApiClient.cs`
3. `IAuth0ManagementApiClient.cs`
4. Auth0 UI provider (if exists)

### Packages to Remove
- `Auth0.AspNetCore.Authentication`
- `Auth0.ManagementApi`
- `@auth0/auth0-react` (UI)

---

## Estimated Timeline

- **Phase 1 (Settings):** 30 minutes
- **Phase 2 (Services):** 2 hours
- **Phase 3 (Authorization):** 1 hour
- **Phase 4 (Endpoints):** 1.5 hours
- **Phase 5 (Program.cs):** 1 hour
- **Phase 6 (Migration):** 30 minutes
- **Phase 7 (UI):** 2.5 hours
- **Phase 8 (Config):** 30 minutes
- **Phase 9 (Cleanup):** 30 minutes
- **Phase 10 (Testing/Docs):** 1.5 hours

**Total:** ~11-12 hours of focused development

---

## Implementation Order

1. Start with Phase 1 (simplify models)
2. Phase 2 (create services - foundation)
3. Phase 3 (update authorization)
4. Phase 5 (update Program.cs with conditional auth)
5. Phase 4 (create endpoints)
6. Phase 6 (database migration)
7. Phase 7 (UI updates)
8. Phase 8 (configuration)
9. Phase 9 (cleanup Auth0)
10. Phase 10 (testing & documentation)

---

## Key Design Decisions

### JWT Token Structure
```json
{
  "sub": "user-id",
  "email": "user@example.com",
  "name": "Full Name",
  "permissions": ["read:user", "write:user", ...],
  "roles": ["Admin"],
  "iat": 1234567890,
  "exp": 1234571490,
  "iss": "storage-labels-api",
  "aud": "storage-labels-ui"
}
```

### Permission Model
- **Permissions** are granular actions (e.g., "read:user", "write:encryption-keys")
- **Roles** are collections of permissions (e.g., "Admin" has all permissions)
- Users can have multiple roles
- Roles are mapped to permissions at token generation time
- UI checks permissions via `hasPermission()` function

### No Auth Mode Behavior
- All API endpoints are accessible without authentication
- User info endpoint returns a default "anonymous" user with all permissions
- UI displays a warning banner indicating no authentication is active
- Suitable only for trusted networks (home lab, internal network)

### Local Auth Flow
1. User submits login form (username + password)
2. API validates credentials using ASP.NET Identity
3. API generates JWT token with user claims + permissions
4. UI stores JWT token in localStorage
5. UI includes token in Authorization header for all API requests
6. API validates JWT token on each request
7. Logout clears token from localStorage

### Migration Strategy
- Existing Auth0 users are NOT migrated (Auth0 support completely removed)
- Fresh start with local authentication
- First run creates default admin user
- Admins can create additional users or allow self-registration
