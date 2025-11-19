# storage-labels

## Project Overview

The **storage-labels** project is a full-stack application suite designed to streamline the management, tracking, and retrieval of physical storage items using modern web and cloud technologies. It consists of two main components:

- **storage-labels-api**: A .NET 9.0 Web API backend that provides endpoints for managing storage locations, boxes, items, users, and supports advanced search features including QR code scanning. It uses Mediator and Ardalis.Result for clean architecture and robust error handling. Features end-to-end encryption for stored images with automated key rotation.
- **storage-labels-ui**: A React 18 web application (built with Parcel and Material-UI v7) that offers a responsive, user-friendly interface for interacting with storage data. It features global search (by text or QR code), real-time notifications, mobile camera support for QR scanning, and encryption key management.

## Key Features

- **Global Search**: Instantly search for boxes, items, or locations by text or by scanning QR codes. Results float above content and clear automatically when the search box is cleared.
- **QR Code Integration**: Use your device's camera to scan QR codes for fast item lookup and navigation.
- **End-to-End Encryption**: All uploaded images are encrypted at rest using AES-256-GCM encryption with versioned encryption keys.
- **Encryption Key Management**: Web-based UI for creating, activating, and retiring encryption keys with role-based access control.
- **Automated Key Rotation**: Background service automatically re-encrypts images when new encryption keys are activated, with real-time progress tracking.
- **Unencrypted Image Migration**: Seamlessly encrypt existing unencrypted images with a single click.
- **Context Providers**: React Context is used for global state management of search and location data, ensuring efficient data sharing and navigation across views.
- **Responsive UI**: Optimized for both desktop and mobile, with intuitive navigation and clear feedback via snackbars.
- **Code Quality**: Comprehensive unit test coverage (26 tests) with xUnit, FluentAssertions, and Moq; enforced ESLint rules to maintain clean, maintainable code.

## Architecture

- **Backend**: .NET 9.0, Mediator, Ardalis.Result, Entity Framework Core, PostgreSQL, RESTful endpoints, System.IO.Abstractions for testable file operations, ASP.NET Core Identity
- **Frontend**: React 18, Parcel, Material-UI v7, @yudiel/react-qr-scanner, React Context API
- **Database**: PostgreSQL 17 (as of v2.0.0)
- **Testing**: xUnit, FluentAssertions, Moq, System.IO.Abstractions.TestingHelpers for unit tests; ESLint for code quality
- **Security**: AES-256-GCM encryption, Local authentication with JWT tokens, role-based access control

## Security & Encryption

### Image Encryption (v2.2.0+)

All uploaded images are encrypted at rest using **AES-256-GCM** (Advanced Encryption Standard with Galois/Counter Mode):

- **Algorithm**: AES-256-GCM
- **Key Size**: 256 bits (32 bytes)
- **Initialization Vector**: 96 bits (12 bytes) - unique per image
- **Authentication Tag**: 128 bits (16 bytes) - ensures data integrity

### Encryption Key Management

- **Versioned Keys**: Create multiple encryption keys with version tracking
- **Key Lifecycle**: Keys progress through Created → Active → Deprecated → Retired states
- **Automatic Rotation**: Background service re-encrypts images when activating new keys
- **Migration Support**: Encrypt existing unencrypted images with active key
- **Progress Tracking**: Real-time rotation progress via API endpoints
- **Statistics**: View encryption key usage (image count, total size)

### Key Rotation Process

1. Create a new encryption key (status: Created)
2. Activate the key (triggers automatic rotation if enabled)
3. Background service re-encrypts all images from old key to new key
4. Old key transitions to Deprecated, then Retired when no longer in use
5. Monitor progress via `/api/encryption-keys/rotations` endpoint

### Authentication & Authorization

**storage-labels** supports two authentication modes:

#### Local Authentication (Default)
- **ASP.NET Core Identity**: Built-in user management with secure password hashing
- **JWT Tokens**: Stateless authentication using JSON Web Tokens
- **Registration**: Optional self-registration (can be disabled)
- **Password Requirements**: Configurable complexity rules (min length, uppercase, lowercase, digits)
- **Account Security**: Lockout protection after failed login attempts

#### No Authentication Mode
- **Trusted Networks**: Suitable for home labs or isolated networks
- **Full Access**: All users have complete permissions without login
- **Development**: Useful for development and testing environments
- **⚠️ Warning**: Only use in completely trusted environments

#### User Roles & Permissions

Three built-in roles with different permission levels:

1. **Admin** - Full system access
   - All read and write permissions
   - User management capabilities
   - Encryption key management
   - First registered user automatically becomes Admin

2. **Auditor** - Read-only access
   - View all data (users, locations, items, encryption keys)
   - Cannot create, modify, or delete data
   - Useful for compliance and monitoring

3. **User** - Standard access
   - Manage own data (locations, boxes, items)
   - Upload and view images
   - No administrative capabilities

#### First User Setup

The **first user registered** in the system automatically receives the **Admin** role. This ensures you can bootstrap the system without external tools.

**Steps:**
1. Start the application with authentication mode set to `Local`
2. Navigate to the registration page
3. Register your admin account
4. You'll automatically have full administrative access
5. Additional users registered later will have the standard User role

**Note**: If `AllowRegistration` is disabled after initial setup, only Admins can create new user accounts (future feature).

### Password Management

- **Self-Service**: Users can change their own password from Preferences
- **Admin Reset**: Admins can reset any user's password (invalidates existing sessions)
- **Security**: Password changes require current password verification
- **Token Invalidation**: Admin password resets automatically log out the affected user

## Database Migration (v2.0.0)

Version 2.0.0 introduces a breaking change by migrating from Microsoft SQL Server to PostgreSQL. This change provides:
- Better Docker/container support
- Easier deployment on platforms like TrueNAS SCALE
- Open-source database with no licensing concerns
- Lowercase table naming convention for PostgreSQL best practices

**Note**: If you're upgrading from v1.x, you'll need to migrate your data. See [VERSIONING.md](VERSIONING.md) for details.

## Getting Started

### Using Docker Compose

1. Copy `docker-compose-custom-config.yaml` to `docker-compose.yml`
2. Create a `.env` file:
   ```bash
   # PostgreSQL Database Configuration
   POSTGRES_PASSWORD=your_secure_password_here
   
   # JWT Secret (generate with: openssl rand -base64 32)
   JWT_SECRET=your-secure-random-key-here-minimum-32-characters-long
   
   # Authentication Mode: Local or None
   AUTHENTICATION_MODE=Local
   ```
3. Run: `docker-compose up -d`
4. Navigate to the application URL
5. **Register the first user** - this account will automatically become the Admin
6. Additional users can register (if enabled) or be created by admins

### Generating a Secure JWT Secret

Use one of these methods to generate a secure JWT secret:

```bash
# Using OpenSSL (recommended)
openssl rand -base64 32

# Using PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | % {[char]$_})

# Using Python
python -c "import secrets; print(secrets.token_urlsafe(32))"
```

### Authentication Configuration

Configure authentication in `appsettings.json` or via environment variables:

#### Local Authentication Settings

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

#### No Authentication Mode

To disable authentication (trusted networks only):

```json
{
  "Authentication": {
    "Mode": "None"
  }
}
```

Or via environment variable:
```bash
AUTHENTICATION_MODE=None
```

⚠️ **Warning**: No Auth mode should only be used in completely trusted environments (home networks, isolated VLANs, etc.)

### Security Best Practices

1. **Change Default JWT Secret**: Never use the default JWT secret in production
2. **Use Strong Passwords**: Enforce password complexity requirements
3. **Enable Lockout**: Protect against brute force attacks
4. **HTTPS Only**: Always use HTTPS in production environments
5. **Regular Updates**: Keep dependencies and system packages updated
6. **Backup Encryption Keys**: Store encryption keys securely and maintain backups

### Development Setup

See the individual folders (`storage-labels-api`, `storage-labels-ui`) for detailed setup and usage instructions.