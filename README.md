# storage-labels

## Project Overview

The **storage-labels** project is a full-stack application suite designed to streamline the management, tracking, and retrieval of physical storage items using modern web and cloud technologies. It consists of three main components:

- **storage-labels-api**: A .NET 9.0 Web API backend that provides endpoints for managing storage locations, boxes, items, users, and supports advanced search features including QR code scanning. It uses MediatR and Ardalis.Result for clean architecture and robust error handling.
- **storage-labels-ui**: A React 18 web application (built with Parcel and Material-UI v7) that offers a responsive, user-friendly interface for interacting with storage data. It features global search (by text or QR code), real-time notifications, and mobile camera support for QR scanning.
- **storage-labels-k6**: A folder for load testing scripts using k6, ensuring the API's performance and reliability under stress.

## Key Features

- **Global Search**: Instantly search for boxes, items, or locations by text or by scanning QR codes. Results float above content and clear automatically when the search box is cleared.
- **QR Code Integration**: Use your device's camera to scan QR codes for fast item lookup and navigation.
- **Context Providers**: React Context is used for global state management of search and location data, ensuring efficient data sharing and navigation across views.
- **Responsive UI**: Optimized for both desktop and mobile, with intuitive navigation and clear feedback via snackbars.
- **Code Quality**: Enforced ESLint rules to maintain clean, maintainable code (including automatic detection of unused imports and variables).

## Architecture

- **Backend**: .NET 9.0, MediatR, Ardalis.Result, Entity Framework Core, PostgreSQL, RESTful endpoints
- **Frontend**: React 18, Parcel, Material-UI v7, @yudiel/react-qr-scanner, React Context API
- **Database**: PostgreSQL 17 (as of v2.0.0)
- **Testing**: k6 for load testing, ESLint for code quality

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

See the individual folders (`storage-labels-api`, `storage-labels-ui`, `storage-labels-k6`) for detailed setup and usage instructions.