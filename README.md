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

- **Backend**: .NET 9.0, Mediator, Ardalis.Result, Entity Framework Core, PostgreSQL, RESTful endpoints, System.IO.Abstractions for testable file operations
- **Frontend**: React 18, Parcel, Material-UI v7, @yudiel/react-qr-scanner, React Context API
- **Database**: PostgreSQL 17 (as of v2.0.0)
- **Testing**: xUnit, FluentAssertions, Moq, System.IO.Abstractions.TestingHelpers for unit tests; ESLint for code quality
- **Security**: AES-256-GCM encryption, Auth0 authentication and authorization, role-based access control

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

- **Auth0 Integration**: Secure user authentication
- **Role-Based Access Control**: Permission-based features (`read:encryption-keys`, `write:encryption-keys`)
- **User-Specific Data**: Each user's data is isolated and secure

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
2. Create a `.env` file with your secrets:
   ```env
   POSTGRES_PASSWORD=your_secure_password
   AUTH0_CLIENT_SECRET=your_auth0_secret
   ```
3. Run: `docker-compose up -d`

### Development Setup

See the individual folders (`storage-labels-api`, `storage-labels-ui`) for detailed setup and usage instructions.