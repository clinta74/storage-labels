# Implementation Guide: v3.0.0 Flexible Authentication

**Branch:** `feature/v3.0.0-flexible-auth`  
**Status:** In Progress (Phase 1 Complete)  
**Last Updated:** November 16, 2025

## Overview

This guide tracks the implementation of flexible authentication for v3.0.0, allowing self-hosting with multiple authentication options:
- **Local** - Built-in ASP.NET Identity (default for self-hosting)
- **Auth0** - Existing Auth0 integration (backward compatibility)
- **OIDC** - Generic OpenID Connect (Authentik, Keycloak, etc.)
- **None** - No authentication for trusted networks
- **External OAuth** - Google, Microsoft, GitHub social login

## ✅ Completed (Phase 1 - Foundation)

### 1. NuGet Packages Added
- ✅ `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.0
- ✅ `OpenIddict.AspNetCore` 6.0.0 (auto-upgraded from 5.9.0)
- ✅ `Microsoft.AspNetCore.Authentication.Google` 9.0.0
- ✅ `Microsoft.AspNetCore.Authentication.MicrosoftAccount` 9.0.0
- ✅ `AspNet.Security.OAuth.GitHub` 9.0.0

### 2. Database Models Created
- ✅ `ApplicationUser.cs` - Extends `IdentityUser` with:
  - FullName, ProfilePictureUrl
  - ExternalProvider, ExternalProviderId (for OAuth)
  - CreatedAt, UpdatedAt, IsActive
  
- ✅ `ApplicationRole.cs` - Extends `IdentityRole` with:
  - Description, CreatedAt

### 3. DbContext Updated
- ✅ `StorageLabelsDbContext` now extends `IdentityDbContext<ApplicationUser, ApplicationRole, string>`
- ✅ Configured lowercase table names for PostgreSQL convention
- ✅ Maintained backward compatibility with existing `User` table (renamed dbset to use `new` keyword)
- ✅ Identity tables: aspnetusers, aspnetroles, aspnetuserroles, etc.

### 4. Configuration Models
- ✅ `AuthenticationSettings.cs` created with:
  - `AuthenticationMode` enum (None, Local, Auth0, OIDC)
  - `LocalAuthSettings` - password policies, lockout, registration
  - `OIDCSettings` - generic OIDC provider config
  - `ExternalProvidersSettings` - Google, Microsoft, GitHub
  - `OAuthProviderSettings` - reusable OAuth config
  - Integrated with existing `Auth0Settings.cs`

### 5. Build Status
- ✅ Project builds successfully with all new dependencies
- ✅ No breaking changes to existing code
- ✅ Backward compatible with current Auth0 implementation

## 🚧 TODO (Phase 2 - Services & Logic)

### 5. Authentication Service Abstraction
**Files to Create:**
```
storage-labels-api/Services/Authentication/
├── IAuthenticationService.cs
├── LocalAuthenticationService.cs
├── Auth0AuthenticationService.cs
├── OIDCAuthenticationService.cs
└── NoAuthenticationService.cs
```

**Interface Structure:**
```csharp
public interface IAuthenticationService
{
    Task<Result<AuthenticationResult>> LoginAsync(LoginRequest request);
    Task<Result<AuthenticationResult>> RegisterAsync(RegisterRequest request);
    Task<Result> LogoutAsync(string userId);
    Task<Result<UserInfo>> GetUserInfoAsync(string userId);
    Task<Result<AuthenticationResult>> ExternalLoginAsync(ExternalLoginRequest request);
}
```

### 6. Update Program.cs
**Location:** `storage-labels-api/Program.cs`

**Tasks:**
- [ ] Load `AuthenticationSettings` from configuration
- [ ] Add conditional Identity configuration based on mode
- [ ] Configure JWT Bearer for API tokens
- [ ] Add external provider authentication conditionally
- [ ] Register authentication services based on mode
- [ ] Configure cookie authentication for local mode
- [ ] Maintain Auth0 configuration for backward compatibility

**Example Pattern:**
```csharp
var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>();

switch (authSettings.Mode)
{
    case AuthenticationMode.Local:
        // Configure Identity + JWT
        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<StorageLabelsDbContext>();
        break;
    case AuthenticationMode.Auth0:
        // Use existing Auth0 config
        break;
    // ... etc
}
```

### 7. Create Authentication Endpoints
**Files to Create:**
```
storage-labels-api/Endpoints/
├── MapAuthentication.cs
└── MapAccountManagement.cs

storage-labels-api/Handlers/Authentication/
├── LoginHandler.cs
├── RegisterHandler.cs
├── LogoutHandler.cs
├── ExternalLoginCallbackHandler.cs
├── RefreshTokenHandler.cs
└── ForgotPasswordHandler.cs
```

**Endpoints:**
- `POST /api/auth/login` - Local username/password
- `POST /api/auth/register` - New user registration
- `POST /api/auth/logout` - Logout
- `GET /api/auth/external/{provider}` - Initiate OAuth flow
- `GET /api/auth/external/callback` - OAuth callback
- `POST /api/auth/refresh` - Refresh JWT token
- `GET /api/auth/config` - Return auth mode to UI

### 8. Update Authorization Handlers
**Files to Update:**
- `storage-labels-api/Authorization/HasScopeHandler.cs`
- `storage-labels-api/Authorization/HasScopeRequirement.cs`

**Tasks:**
- [ ] Support both Auth0 scopes and local Identity claims
- [ ] Add role-based authorization support
- [ ] Create claim transformation for different auth modes
- [ ] Maintain backward compatibility with Auth0 tokens

### 9. Database Migration
**Commands to Run:**
```bash
cd storage-labels-api

# Create migration for Identity tables
dotnet ef migrations add AddIdentityTables --context StorageLabelsDbContext

# Review migration file before applying
# Should create: aspnetusers, aspnetroles, aspnetuserroles, etc.

# Apply migration (do this in development first!)
dotnet ef database update --context StorageLabelsDbContext
```

**Important:**
- Migration will add 7 new Identity tables
- Existing `users` table remains unchanged (Auth0 users)
- No data migration needed - systems run in parallel

## 🎨 TODO (Phase 3 - UI & Configuration)

### 10. Update UI Authentication
**Files to Update:**
```
storage-labels-ui/src/auth/
├── auth-provider.tsx (update to detect auth mode)
├── local-auth.tsx (new - local login form)
└── external-auth.tsx (new - OAuth buttons)

storage-labels-ui/src/api/
└── auth.ts (new - local auth endpoints)
```

**Tasks:**
- [ ] Add endpoint to fetch auth config from API
- [ ] Create local login form component
- [ ] Add OAuth provider buttons (Google, Microsoft, GitHub)
- [ ] Update auth provider to handle multiple modes
- [ ] Maintain Auth0 flow for backward compatibility
- [ ] Add registration form for local auth

### 11. Update appsettings.json
**File:** `storage-labels-api/appsettings.json`

**Add Section:**
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
    },
    "Auth0": {
      "Enabled": false,
      "Domain": "",
      "ClientId": "",
      "ClientSecret": "",
      "Audience": ""
    },
    "OIDC": {
      "Enabled": false,
      "Authority": "",
      "ClientId": "",
      "ClientSecret": "",
      "ResponseType": "code",
      "Scopes": ["openid", "profile", "email"]
    },
    "ExternalProviders": {
      "Google": {
        "Enabled": false,
        "ClientId": "",
        "ClientSecret": ""
      },
      "Microsoft": {
        "Enabled": false,
        "ClientId": "",
        "ClientSecret": ""
      },
      "GitHub": {
        "Enabled": false,
        "ClientId": "",
        "ClientSecret": ""
      }
    }
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars-for-hs256",
    "Issuer": "storage-labels-api",
    "Audience": "storage-labels-ui",
    "ExpirationMinutes": 60
  }
}
```

### 12. Update docker-compose.yaml
**File:** `docker-compose-custom-config.yaml`

**Add Environment Variables:**
```yaml
storage-labels-api:
  environment:
    # Authentication Mode: Local, Auth0, OIDC, None
    - Authentication__Mode=Local
    
    # Local Authentication (default)
    - Authentication__Local__Enabled=true
    - Authentication__Local__AllowRegistration=true
    
    # JWT Settings for Local/OIDC modes
    - Jwt__Secret=${JWT_SECRET}
    - Jwt__Issuer=storage-labels-api
    - Jwt__Audience=storage-labels-ui
    - Jwt__ExpirationMinutes=60
    
    # Auth0 (legacy - optional)
    - Authentication__Auth0__Enabled=false
    - Authentication__Auth0__Domain=${AUTH0_DOMAIN}
    - Authentication__Auth0__ClientId=${AUTH0_CLIENT_ID}
    - Authentication__Auth0__ClientSecret=${AUTH0_CLIENT_SECRET}
    - Authentication__Auth0__Audience=${AUTH0_AUDIENCE}
    
    # Generic OIDC Provider (optional)
    - Authentication__OIDC__Enabled=false
    - Authentication__OIDC__Authority=${OIDC_AUTHORITY}
    - Authentication__OIDC__ClientId=${OIDC_CLIENT_ID}
    - Authentication__OIDC__ClientSecret=${OIDC_CLIENT_SECRET}
    
    # Google OAuth (optional)
    - Authentication__ExternalProviders__Google__Enabled=false
    - Authentication__ExternalProviders__Google__ClientId=${GOOGLE_CLIENT_ID}
    - Authentication__ExternalProviders__Google__ClientSecret=${GOOGLE_CLIENT_SECRET}
    
    # Microsoft OAuth (optional)
    - Authentication__ExternalProviders__Microsoft__Enabled=false
    - Authentication__ExternalProviders__Microsoft__ClientId=${MICROSOFT_CLIENT_ID}
    - Authentication__ExternalProviders__Microsoft__ClientSecret=${MICROSOFT_CLIENT_SECRET}
    
    # GitHub OAuth (optional)
    - Authentication__ExternalProviders__GitHub__Enabled=false
    - Authentication__ExternalProviders__GitHub__ClientId=${GITHUB_CLIENT_ID}
    - Authentication__ExternalProviders__GitHub__ClientSecret=${GITHUB_CLIENT_SECRET}
```

### 13. Create .env.example
**File:** `.env.example`

```bash
# JWT Secret (generate with: openssl rand -base64 32)
JWT_SECRET=your-secure-random-key-here-min-32-characters

# Database
POSTGRES_PASSWORD=your-db-password

# Auth0 (optional - for backward compatibility)
AUTH0_DOMAIN=your-domain.auth0.com
AUTH0_CLIENT_ID=your-client-id
AUTH0_CLIENT_SECRET=your-client-secret
AUTH0_AUDIENCE=your-api-audience

# Generic OIDC Provider (optional - Authentik, Keycloak, etc.)
OIDC_AUTHORITY=https://auth.example.com
OIDC_CLIENT_ID=storage-labels
OIDC_CLIENT_SECRET=your-oidc-secret

# Google OAuth (optional)
GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-secret

# Microsoft OAuth (optional)
MICROSOFT_CLIENT_ID=your-microsoft-client-id
MICROSOFT_CLIENT_SECRET=your-microsoft-secret

# GitHub OAuth (optional)
GITHUB_CLIENT_ID=your-github-client-id
GITHUB_CLIENT_SECRET=your-github-secret
```

## 📚 TODO (Phase 4 - Documentation)

### 14. Migration Guide
**File:** `AUTHENTICATION-MIGRATION.md`

**Contents:**
- How to migrate from Auth0-only to flexible auth
- Existing Auth0 users can continue using Auth0
- How to enable local authentication alongside Auth0
- Data migration considerations (none required!)
- Rollback procedures

### 15. README Updates
**File:** `README.md`

**Sections to Add:**
- Authentication configuration overview
- Quick start for each mode (Local, Auth0, OIDC, None)
- How to obtain OAuth credentials:
  - Google Cloud Console setup
  - Microsoft Azure App Registration
  - GitHub OAuth Apps
- Security best practices
- Troubleshooting authentication issues

## 🧪 Testing Checklist

### Local Authentication
- [ ] Register new user
- [ ] Login with username/password
- [ ] Logout
- [ ] Password reset flow
- [ ] Email confirmation (if enabled)
- [ ] Account lockout after failed attempts
- [ ] JWT token generation and validation

### Auth0 (Backward Compatibility)
- [ ] Existing Auth0 flow still works
- [ ] Auth0 tokens accepted
- [ ] Auth0 users can access their data
- [ ] No breaking changes

### External Providers
- [ ] Google OAuth login
- [ ] Microsoft OAuth login
- [ ] GitHub OAuth login
- [ ] Account linking to existing local accounts

### OIDC
- [ ] Connect to Authentik
- [ ] Connect to Keycloak
- [ ] Generic OIDC provider

### No Auth Mode
- [ ] All endpoints accessible
- [ ] Warning displayed in UI
- [ ] Suitable for trusted networks only

## 🔒 Security Considerations

1. **JWT Secret:** Must be at least 32 characters for HS256
2. **HTTPS Required:** All OAuth flows require HTTPS in production
3. **Callback URLs:** Must be registered with each OAuth provider
4. **CORS:** Configure properly for external auth
5. **Password Policies:** Configurable via settings
6. **Rate Limiting:** Consider adding to prevent brute force
7. **Token Expiration:** Configurable JWT expiration
8. **Refresh Tokens:** Implement for better UX

## 📁 File Structure Reference

```
storage-labels-api/
├── Datalayer/
│   ├── Models/
│   │   ├── ApplicationUser.cs ✅
│   │   ├── ApplicationRole.cs ✅
│   │   └── User.cs (legacy Auth0)
│   └── StorageLabelsDbContext.cs ✅
├── Models/
│   ├── DTO/
│   │   └── Authentication/ (TODO)
│   │       ├── LoginRequest.cs
│   │       ├── RegisterRequest.cs
│   │       ├── AuthenticationResult.cs
│   │       └── UserInfo.cs
│   └── Settings/
│       ├── AuthenticationSettings.cs ✅
│       └── Auth0Settings.cs (existing)
├── Services/
│   ├── Authentication/ (TODO)
│   │   ├── IAuthenticationService.cs
│   │   ├── LocalAuthenticationService.cs
│   │   ├── Auth0AuthenticationService.cs
│   │   ├── OIDCAuthenticationService.cs
│   │   └── NoAuthenticationService.cs
│   └── IAuth0ManagementApiClient.cs (existing)
├── Endpoints/
│   ├── MapAuthentication.cs (TODO)
│   └── MapAccountManagement.cs (TODO)
├── Handlers/
│   └── Authentication/ (TODO)
├── Authorization/
│   ├── HasScopeHandler.cs (needs update)
│   └── HasScopeRequirement.cs (needs update)
├── Migrations/
│   └── AddIdentityTables.cs (TODO)
└── Program.cs (needs major update)
```

## 🎯 Next Session Checklist

When resuming work:

1. ✅ Review this guide
2. ✅ Check current branch: `feature/v3.0.0-flexible-auth`
3. ✅ Verify last commit: "feat(auth): Add ASP.NET Core Identity foundation"
4. ⏭️ Start with Phase 2, Task 5: Create authentication service interfaces
5. ⏭️ Then move to Program.cs configuration
6. ⏭️ Create endpoints and handlers
7. ⏭️ Generate and apply database migration
8. ⏭️ Update UI components
9. ⏭️ Update configuration files
10. ⏭️ Write documentation

## 🚀 Quick Start Commands

```bash
# Switch to feature branch
git checkout feature/v3.0.0-flexible-auth

# Check current status
git log --oneline -5
git status

# Build to verify
cd storage-labels-api
dotnet build

# Create migration (when ready)
dotnet ef migrations add AddIdentityTables

# Run in development
dotnet run

# View todo list
# See this file or run: cat IMPLEMENTATION-GUIDE-v3.0.0.md
```

## 📊 Progress Tracker

- **Overall:** 33% Complete (5/15 major tasks)
- **Phase 1 (Foundation):** 100% Complete ✅
- **Phase 2 (Services & Logic):** 0% Complete
- **Phase 3 (UI & Config):** 0% Complete
- **Phase 4 (Documentation):** 0% Complete

---

**Important Notes:**
- All changes maintain backward compatibility with Auth0
- No data migration required - systems run in parallel
- PostgreSQL keeps us on .NET 9.0 (not .NET 10)
- OpenIddict auto-upgraded from 5.9.0 to 6.0.0 (warning is normal)
- Build is clean and tests should pass

**Estimated Remaining Time:**
- Phase 2: 4-6 hours
- Phase 3: 3-4 hours
- Phase 4: 2-3 hours
- **Total:** 9-13 hours of focused development

Good luck! 🎉
