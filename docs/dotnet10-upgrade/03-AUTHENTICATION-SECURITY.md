# Authentication & Security Features in .NET 10

## Overview
This guide covers .NET 10's enhanced authentication and security features including passkeys (WebAuthn), improved two-factor authentication, token revocation, and security best practices.

---

## 1. Passkeys Support (WebAuthn/FIDO2)

### What It Is
Passkeys are a passwordless authentication method using public-key cryptography. Users authenticate with biometrics (fingerprint, Face ID) or device PIN instead of passwords.

### Why Passkeys Matter

**Problems with passwords:**
- Users forget them (30-40% of support tickets)
- Weak passwords easily guessed
- Phishing attacks steal passwords
- Password reuse across sites
- Need password resets

**Benefits of passkeys:**
- ✅ No passwords to remember
- ✅ Phishing-resistant (cryptographic proof)
- ✅ No password reuse
- ✅ Faster login (biometric)
- ✅ Cross-platform (sync via iCloud/Google)

### How Passkeys Work

```
Registration:
1. User clicks "Create passkey"
2. Browser/OS prompts for biometric
3. Device generates key pair (public/private)
4. Server stores public key only
5. Private key never leaves device

Authentication:
1. User visits site
2. Server sends challenge (random data)
3. Device signs challenge with private key
4. Server verifies with public key
✓ User authenticated!
```

### Implementation for Storage Labels

#### Step 1: Install Package

```bash
dotnet add package Fido2.NetFramework
```

#### Step 2: Configure Services

**Program.cs:**
```csharp
builder.Services.AddFido2(options =>
{
    options.ServerDomain = builder.Configuration["Fido2:ServerDomain"] ?? "localhost";
    options.ServerName = "Storage Labels";
    options.Origins = builder.Configuration.GetSection("Fido2:Origins").Get<HashSet<string>>() 
        ?? new HashSet<string> { "https://localhost:5001" };
    options.TimestampDriftTolerance = 300000; // 5 minutes
    options.MDSAccessKey = builder.Configuration["Fido2:MDSAccessKey"]; // Optional: metadata service
});
```

**appsettings.json:**
```json
{
  "Fido2": {
    "ServerDomain": "storage-labels.example.com",
    "Origins": [
      "https://storage-labels.example.com",
      "https://app.storage-labels.com"
    ]
  }
}
```

#### Step 3: Database Model

```csharp
public class UserPasskey
{
    public Guid PasskeyId { get; set; }
    public required string UserId { get; set; }
    public required byte[] CredentialId { get; set; } // Unique ID for this credential
    public required byte[] PublicKey { get; set; } // User's public key
    public required byte[] UserHandle { get; set; } // User identifier
    public required string CredType { get; set; } // "public-key"
    public uint SignatureCounter { get; set; } // Anti-replay
    public required string CredentialDeviceType { get; set; } // "platform" or "cross-platform"
    public required string FriendlyName { get; set; } // "John's iPhone"
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
```

#### Step 4: Registration Endpoint

**Create registration options:**

```csharp
public record CreatePasskeyOptionsRequest(string UserId);
public record CreatePasskeyOptionsResponse(string OptionsJson);

public class CreatePasskeyOptionsHandler : IRequestHandler<CreatePasskeyOptionsRequest, Result<CreatePasskeyOptionsResponse>>
{
    private readonly IFido2 _fido2;
    private readonly UserManager<User> _userManager;
    private readonly StorageLabelsDbContext _dbContext;
    
    public async ValueTask<Result<CreatePasskeyOptionsResponse>> Handle(
        CreatePasskeyOptionsRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return Result<CreatePasskeyOptionsResponse>.NotFound();
        
        // Get existing credentials for this user
        var existingKeys = await _dbContext.UserPasskeys
            .Where(p => p.UserId == request.UserId)
            .Select(p => new PublicKeyCredentialDescriptor(p.CredentialId))
            .ToListAsync(cancellationToken);
        
        // Create registration options
        var userFido = new Fido2User
        {
            DisplayName = user.Email!,
            Name = user.Email!,
            Id = Encoding.UTF8.GetBytes(user.Id)
        };
        
        var authenticatorSelection = new AuthenticatorSelection
        {
            RequireResidentKey = false,
            UserVerification = UserVerificationRequirement.Preferred,
            AuthenticatorAttachment = null // Allow both platform and cross-platform
        };
        
        var options = _fido2.RequestNewCredential(
            userFido,
            existingKeys,
            authenticatorSelection,
            AttestationConveyancePreference.None);
        
        // Store challenge temporarily (use distributed cache in production)
        await _cache.SetStringAsync(
            $"fido2-challenge:{user.Id}",
            options.Challenge.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        
        return Result<CreatePasskeyOptionsResponse>.Success(
            new CreatePasskeyOptionsResponse(options.ToJson()));
    }
}
```

**Verify registration:**

```csharp
public record RegisterPasskeyRequest(
    string UserId,
    string AttestationJson,
    string FriendlyName);

public class RegisterPasskeyHandler : IRequestHandler<RegisterPasskeyRequest, Result>
{
    public async ValueTask<Result> Handle(
        RegisterPasskeyRequest request, 
        CancellationToken cancellationToken)
    {
        // Get stored challenge
        var challengeString = await _cache.GetStringAsync($"fido2-challenge:{request.UserId}");
        if (string.IsNullOrEmpty(challengeString))
            return Result.Invalid(new ValidationError("Challenge expired"));
        
        // Parse attestation response from client
        var attestationResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(
            request.AttestationJson)!;
        
        // Verify attestation
        var success = await _fido2.MakeNewCredentialAsync(
            attestationResponse,
            new CredentialCreateOptions
            {
                Challenge = Convert.FromBase64String(challengeString),
                // ... other options from registration
            },
            async (args, cancellationToken) =>
            {
                // Check if credential ID already exists
                var exists = await _dbContext.UserPasskeys
                    .AnyAsync(p => p.CredentialId == args.CredentialId, cancellationToken);
                return !exists;
            },
            cancellationToken);
        
        if (success.Status != "ok")
            return Result.Invalid(new ValidationError("Passkey registration failed"));
        
        // Store passkey
        var passkey = new UserPasskey
        {
            PasskeyId = Guid.NewGuid(),
            UserId = request.UserId,
            CredentialId = success.Result!.Id,
            PublicKey = success.Result.PublicKey,
            UserHandle = success.Result.User.Id,
            CredType = success.Result.CredType,
            SignatureCounter = success.Result.SignCount,
            CredentialDeviceType = success.Result.DeviceType,
            FriendlyName = request.FriendlyName,
            CreatedAt = DateTime.UtcNow
        };
        
        _dbContext.UserPasskeys.Add(passkey);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // Clear challenge
        await _cache.RemoveAsync($"fido2-challenge:{request.UserId}");
        
        return Result.Success();
    }
}
```

#### Step 5: Authentication Endpoint

**Create assertion options:**

```csharp
public record PasskeyLoginOptionsRequest(string? Email);

public class PasskeyLoginOptionsHandler : IRequestHandler<PasskeyLoginOptionsRequest, Result<string>>
{
    public async ValueTask<Result<string>> Handle(
        PasskeyLoginOptionsRequest request, 
        CancellationToken cancellationToken)
    {
        List<PublicKeyCredentialDescriptor> allowedCredentials;
        
        if (!string.IsNullOrEmpty(request.Email))
        {
            // Email provided - get user's credentials
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result<string>.NotFound();
            
            allowedCredentials = await _dbContext.UserPasskeys
                .Where(p => p.UserId == user.Id)
                .Select(p => new PublicKeyCredentialDescriptor(p.CredentialId))
                .ToListAsync(cancellationToken);
        }
        else
        {
            // No email - discoverable credentials (allow device to choose)
            allowedCredentials = new List<PublicKeyCredentialDescriptor>();
        }
        
        var options = _fido2.GetAssertionOptions(
            allowedCredentials,
            UserVerificationRequirement.Preferred);
        
        // Store challenge
        var challengeKey = $"fido2-auth-challenge:{Guid.NewGuid()}";
        await _cache.SetStringAsync(
            challengeKey,
            options.Challenge.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        
        // Return challenge key to client
        var response = new
        {
            options = options.ToJson(),
            challengeKey
        };
        
        return Result<string>.Success(JsonSerializer.Serialize(response));
    }
}
```

**Verify assertion:**

```csharp
public record PasskeyLoginRequest(
    string AssertionJson,
    string ChallengeKey);

public class PasskeyLoginHandler : IRequestHandler<PasskeyLoginRequest, Result<LoginResponse>>
{
    public async ValueTask<Result<LoginResponse>> Handle(
        PasskeyLoginRequest request, 
        CancellationToken cancellationToken)
    {
        // Get stored challenge
        var challengeString = await _cache.GetStringAsync(request.ChallengeKey);
        if (string.IsNullOrEmpty(challengeString))
            return Result<LoginResponse>.Invalid(new ValidationError("Challenge expired"));
        
        // Parse assertion from client
        var assertion = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(
            request.AssertionJson)!;
        
        // Find passkey
        var passkey = await _dbContext.UserPasskeys
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.CredentialId == assertion.Id, cancellationToken);
        
        if (passkey == null)
            return Result<LoginResponse>.NotFound("Passkey not found");
        
        // Verify assertion
        var success = await _fido2.MakeAssertionAsync(
            assertion,
            new AssertionOptions
            {
                Challenge = Convert.FromBase64String(challengeString),
                // ... other options from assertion
            },
            passkey.PublicKey,
            passkey.SignatureCounter,
            async (args, cancellationToken) =>
            {
                // Verify user
                return passkey.UserHandle.SequenceEqual(args.UserHandle);
            },
            cancellationToken);
        
        if (success.Status != "ok")
            return Result<LoginResponse>.Invalid(new ValidationError("Authentication failed"));
        
        // Update signature counter and last used
        passkey.SignatureCounter = success.SignCount;
        passkey.LastUsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        // Clear challenge
        await _cache.RemoveAsync(request.ChallengeKey);
        
        // Generate JWT token
        var token = await _jwtTokenService.GenerateTokenAsync(passkey.User);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
            passkey.User.Id,
            httpContext);
        
        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = 3600
        });
    }
}
```

#### Step 6: Frontend Integration

**TypeScript/JavaScript:**

```typescript
// Register passkey
async function registerPasskey() {
  // 1. Get options from server
  const optionsResponse = await fetch('/api/auth/passkey/register-options', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId: currentUser.id })
  });
  const { optionsJson } = await optionsResponse.json();
  const options = JSON.parse(optionsJson);
  
  // 2. Create credential with WebAuthn API
  const credential = await navigator.credentials.create({
    publicKey: {
      ...options,
      challenge: base64ToBuffer(options.challenge),
      user: {
        ...options.user,
        id: base64ToBuffer(options.user.id)
      }
    }
  }) as PublicKeyCredential;
  
  // 3. Send attestation to server
  const attestation = {
    id: credential.id,
    rawId: bufferToBase64(credential.rawId),
    response: {
      attestationObject: bufferToBase64(credential.response.attestationObject),
      clientDataJSON: bufferToBase64(credential.response.clientDataJSON)
    },
    type: credential.type
  };
  
  await fetch('/api/auth/passkey/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      userId: currentUser.id,
      attestationJson: JSON.stringify(attestation),
      friendlyName: deviceName
    })
  });
}

// Login with passkey
async function loginWithPasskey(email?: string) {
  // 1. Get assertion options
  const optionsResponse = await fetch('/api/auth/passkey/login-options', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email })
  });
  const { options: optionsJson, challengeKey } = await optionsResponse.json();
  const options = JSON.parse(optionsJson);
  
  // 2. Get assertion from authenticator
  const credential = await navigator.credentials.get({
    publicKey: {
      ...options,
      challenge: base64ToBuffer(options.challenge)
    }
  }) as PublicKeyCredential;
  
  // 3. Send assertion to server
  const assertion = {
    id: credential.id,
    rawId: bufferToBase64(credential.rawId),
    response: {
      authenticatorData: bufferToBase64(credential.response.authenticatorData),
      clientDataJSON: bufferToBase64(credential.response.clientDataJSON),
      signature: bufferToBase64(credential.response.signature),
      userHandle: bufferToBase64(credential.response.userHandle)
    },
    type: credential.type
  };
  
  const loginResponse = await fetch('/api/auth/passkey/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      assertionJson: JSON.stringify(assertion),
      challengeKey
    })
  });
  
  const { accessToken, refreshToken } = await loginResponse.json();
  // Store tokens and redirect
}
```

### When to Use Passkeys

✅ **Use When:**
- Modern user base (Chrome 67+, Safari 16+, Edge 85+)
- Security is priority (phishing protection)
- Want to reduce password support burden
- Mobile-first application

⚠️ **Considerations:**
- Not all browsers support it yet
- Backup authentication method needed (email magic link)
- Users need education (new concept)
- Requires HTTPS

---

## 2. Enhanced Two-Factor Authentication

### What It Is
.NET 10 improves 2FA with better TOTP (Time-based One-Time Password) handling, backup codes, and recovery options.

### Implementation

#### Step 1: Enable 2FA

```csharp
public record Enable2FARequest(string UserId);
public record Enable2FAResponse(
    string Secret,
    string QrCodeUri,
    string[] BackupCodes);

public class Enable2FAHandler : IRequestHandler<Enable2FARequest, Result<Enable2FAResponse>>
{
    private readonly UserManager<User> _userManager;
    
    public async ValueTask<Result<Enable2FAResponse>> Handle(
        Enable2FARequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return Result<Enable2FAResponse>.NotFound();
        
        // Generate secret key
        var secret = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(secret))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            secret = await _userManager.GetAuthenticatorKeyAsync(user);
        }
        
        // Generate QR code URI for authenticator apps
        var appName = "StorageLabels";
        var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(appName)}:{Uri.EscapeDataString(user.Email!)}?secret={secret}&issuer={Uri.EscapeDataString(appName)}&digits=6";
        
        // Generate backup codes
        var backupCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        
        return Result<Enable2FAResponse>.Success(new Enable2FAResponse(
            Secret: FormatSecret(secret),
            QrCodeUri: qrCodeUri,
            BackupCodes: backupCodes!.ToArray()));
    }
    
    private static string FormatSecret(string secret)
    {
        // Format as: XXXX-XXXX-XXXX-XXXX for readability
        return string.Join("-", Enumerable.Range(0, secret.Length / 4)
            .Select(i => secret.Substring(i * 4, 4)));
    }
}
```

#### Step 2: Verify and Activate

```csharp
public record Verify2FARequest(string UserId, string Code);

public class Verify2FAHandler : IRequestHandler<Verify2FARequest, Result>
{
    public async ValueTask<Result> Handle(
        Verify2FARequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return Result.NotFound();
        
        // Verify code
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            request.Code);
        
        if (!isValid)
            return Result.Invalid(new ValidationError("Invalid code"));
        
        // Enable 2FA
        await _userManager.SetTwoFactorEnabledAsync(user, true);
        
        _logger.TwoFactorEnabled(user.Id);
        
        return Result.Success();
    }
}
```

#### Step 3: Login with 2FA

```csharp
public record Login2FARequest(
    string UserId,
    string Code,
    bool RememberDevice);

public class Login2FAHandler : IRequestHandler<Login2FARequest, Result<LoginResponse>>
{
    public async ValueTask<Result<LoginResponse>> Handle(
        Login2FARequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return Result<LoginResponse>.NotFound();
        
        // Try authenticator code first
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            request.Code);
        
        // If not valid, try backup code
        if (!isValid)
        {
            var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, request.Code);
            if (!result.Succeeded)
                return Result<LoginResponse>.Invalid(new ValidationError("Invalid code"));
            
            _logger.BackupCodeUsed(user.Id);
        }
        
        // Generate tokens
        var token = await _jwtTokenService.GenerateTokenAsync(user);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
            user.Id,
            _httpContext,
            rememberMe: request.RememberDevice);
        
        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresIn = 3600
        });
    }
}
```

#### Step 4: Recovery Options

```csharp
public class Manage2FAHandler
{
    // Disable 2FA (requires password confirmation)
    public async Task<Result> Disable2FAAsync(string userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.NotFound();
        
        // Verify password
        var isValidPassword = await _userManager.CheckPasswordAsync(user, password);
        if (!isValidPassword)
            return Result.Invalid(new ValidationError("Invalid password"));
        
        // Disable 2FA
        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        
        return Result.Success();
    }
    
    // Generate new backup codes
    public async Task<Result<string[]>> GenerateNewBackupCodesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<string[]>.NotFound();
        
        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return Result<string[]>.Success(codes!.ToArray());
    }
    
    // Check remaining backup codes
    public async Task<Result<int>> GetRemainingBackupCodesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result<int>.NotFound();
        
        var count = await _userManager.CountRecoveryCodesAsync(user);
        return Result<int>.Success(count);
    }
}
```

---

## 3. Token Revocation

### What It Is
Ability to invalidate JWT tokens before they expire, essential for security (logout, account compromise, permission changes).

### The Problem with JWTs

**JWT tokens are stateless:**
- Server doesn't track issued tokens
- Can't revoke before expiration
- Logout doesn't really log out
- Compromised token valid until expiry

### Solution: Token Revocation List

#### Option 1: Database-Based Revocation

**Model:**
```csharp
public class RevokedToken
{
    public Guid Id { get; set; }
    public required string TokenId { get; set; } // JWT "jti" claim
    public required string UserId { get; set; }
    public required DateTime RevokedAt { get; set; }
    public required DateTime ExpiresAt { get; set; }
    public string? Reason { get; set; }
    
    // Index on TokenId for fast lookup
}
```

**Revoke tokens:**
```csharp
public class TokenRevocationService
{
    public async Task RevokeTokenAsync(string tokenId, string reason)
    {
        var token = new RevokedToken
        {
            Id = Guid.NewGuid(),
            TokenId = tokenId,
            UserId = GetUserIdFromToken(tokenId),
            RevokedAt = DateTime.UtcNow,
            ExpiresAt = GetExpirationFromToken(tokenId),
            Reason = reason
        };
        
        _dbContext.RevokedTokens.Add(token);
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task RevokeAllUserTokensAsync(string userId)
    {
        // Get all active tokens for user (from refresh tokens table)
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        
        foreach (var rt in activeTokens)
        {
            await RevokeTokenAsync(rt.TokenId, "User logged out from all devices");
        }
    }
}
```

**Check on each request:**
```csharp
public class TokenRevocationMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tokenId = context.User.FindFirst("jti")?.Value;
            
            if (!string.IsNullOrEmpty(tokenId))
            {
                // Check if token is revoked
                var isRevoked = await _cache.GetOrCreateAsync(
                    $"token-revoked:{tokenId}",
                    async () =>
                    {
                        return await _dbContext.RevokedTokens
                            .AnyAsync(rt => rt.TokenId == tokenId);
                    },
                    TimeSpan.FromMinutes(5)); // Cache for 5 minutes
                
                if (isRevoked)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token has been revoked");
                    return;
                }
            }
        }
        
        await _next(context);
    }
}
```

#### Option 2: Redis-Based Revocation (Faster)

```csharp
public class RedisTokenRevocationService
{
    private readonly IConnectionMultiplexer _redis;
    
    public async Task RevokeTokenAsync(string tokenId, DateTime expiresAt)
    {
        var db = _redis.GetDatabase();
        var ttl = expiresAt - DateTime.UtcNow;
        
        if (ttl > TimeSpan.Zero)
        {
            // Set key with TTL = token expiration
            await db.StringSetAsync(
                $"revoked:{tokenId}",
                "1",
                ttl);
        }
    }
    
    public async Task<bool> IsTokenRevokedAsync(string tokenId)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync($"revoked:{tokenId}");
    }
    
    public async Task RevokeAllUserTokensAsync(string userId)
    {
        var db = _redis.GetDatabase();
        
        // Store revocation timestamp for user
        await db.StringSetAsync(
            $"user-revoke:{userId}",
            DateTime.UtcNow.ToString("O"),
            TimeSpan.FromHours(24)); // Keep for 24 hours
    }
    
    public async Task<bool> IsUserTokensRevokedAsync(string userId, DateTime tokenIssuedAt)
    {
        var db = _redis.GetDatabase();
        var revokeTimeStr = await db.StringGetAsync($"user-revoke:{userId}");
        
        if (revokeTimeStr.HasValue && DateTime.TryParse(revokeTimeStr, out var revokeTime))
        {
            // Token issued before revocation = revoked
            return tokenIssuedAt < revokeTime;
        }
        
        return false;
    }
}
```

### Logout Implementation

```csharp
public record LogoutRequest(bool FromAllDevices = false);

public class LogoutHandler : IRequestHandler<LogoutRequest, Result>
{
    private readonly TokenRevocationService _revocation;
    private readonly RefreshTokenService _refreshTokens;
    private readonly HttpContext _httpContext;
    
    public async ValueTask<Result> Handle(
        LogoutRequest request, 
        CancellationToken cancellationToken)
    {
        var userId = _httpContext.GetUserId();
        var tokenId = _httpContext.User.FindFirst("jti")?.Value;
        
        if (request.FromAllDevices)
        {
            // Revoke all user tokens
            await _revocation.RevokeAllUserTokensAsync(userId);
            
            // Delete all refresh tokens
            await _refreshTokens.RevokeAllUserTokensAsync(userId);
        }
        else
        {
            // Revoke current token only
            if (!string.IsNullOrEmpty(tokenId))
            {
                await _revocation.RevokeTokenAsync(tokenId, "User logged out");
            }
            
            // Delete current refresh token
            var refreshToken = _httpContext.Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _refreshTokens.RevokeTokenAsync(refreshToken);
            }
        }
        
        // Clear cookies
        _httpContext.Response.Cookies.Delete("refreshToken");
        
        return Result.Success();
    }
}
```

---

## Implementation Checklist

### Passkeys (Optional - cutting edge)
- [ ] Install Fido2.NetFramework package
- [ ] Add UserPasskey model and migration
- [ ] Implement registration endpoints
- [ ] Implement authentication endpoints
- [ ] Add frontend WebAuthn integration
- [ ] Test on multiple devices

### 2FA (Recommended)
- [ ] Verify ASP.NET Core Identity 2FA setup
- [ ] Implement enable 2FA endpoint
- [ ] Generate QR codes for authenticator apps
- [ ] Implement backup codes
- [ ] Add 2FA to login flow
- [ ] Add recovery options

### Token Revocation (High Priority)
- [ ] Add RevokedTokens table
- [ ] Implement revocation service (DB or Redis)
- [ ] Add revocation middleware
- [ ] Update logout to revoke tokens
- [ ] Add "logout from all devices"
- [ ] Add token cleanup job

---

## References

- [WebAuthn Guide](https://webauthn.guide/)
- [Fido2.NetFramework](https://github.com/passwordless-lib/fido2-net-lib)
- [ASP.NET Core Identity 2FA](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/mfa)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)

---

*Document created: February 7, 2026*
*For: Storage Labels API .NET 10 Upgrade*
