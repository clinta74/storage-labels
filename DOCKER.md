# Docker Setup and GitHub Actions

## GitHub Secrets Configuration

To enable the GitHub Actions workflows to push Docker images to Docker Hub, you need to add the following secrets to your GitHub repository:

### Adding Secrets

1. Go to your GitHub repository
2. Click on **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Add the following secrets:

#### Required Secrets:

- **DOCKER_USERNAME**: Your Docker Hub username
  - Example: `myusername`
  
- **DOCKER_PASSWORD**: Your Docker Hub access token (recommended) or password
  - To create an access token:
    1. Log in to Docker Hub
    2. Go to Account Settings → Security
    3. Click "New Access Token"
    4. Give it a descriptive name (e.g., "GitHub Actions")
    5. Copy the token and use it as the secret value

## GitHub Actions Workflows

### UI Workflow (`.github/workflows/build-ui.yml`)
- **Triggers**: 
  - Push to `main` branch when files in `storage-labels-ui/` change
  - Pull requests to `main` branch
  - Manual workflow dispatch
- **Output**: Docker image tagged as `{username}/storage-labels-ui:latest`
- **Features**:
  - Multi-platform build (linux/amd64, linux/arm64)
  - Nginx-based production image
  - Build caching for faster builds
  - Automatic tagging (branch, PR, SHA, semver)

### API Workflow (`.github/workflows/build-api.yml`)
- **Triggers**: 
  - Push to `main` branch when files in `storage-labels-api/` change
  - Pull requests to `main` branch
  - Manual workflow dispatch
- **Output**: Docker image tagged as `{username}/storage-labels-api:latest`
- **Features**:
  - Multi-platform build (linux/amd64, linux/arm64)
  - .NET 9.0 runtime
  - Build caching for faster builds
  - Automatic tagging (branch, PR, SHA, semver)

## Docker Images

### UI Image (`storage-labels-ui`)
- **Base Image**: nginx:alpine
- **Exposed Port**: 80
- **Size**: ~50MB (optimized)
- **Features**:
  - Gzip compression
  - Security headers
  - Client-side routing support
  - Static asset caching

### API Image (`storage-labels-api`)
- **Base Image**: mcr.microsoft.com/dotnet/aspnet:9.0
- **Exposed Port**: 8080
- **Size**: ~220MB
- **Features**:
  - Multi-stage build for smaller image
  - .NET 9.0 runtime
  - Optimized for production

## Environment Variables

### UI Environment Variables

The UI requires the following environment variables. Create a `.env` file in `storage-labels-ui/`:

```env
# API Configuration
API_URL=http://localhost:5000

# Auth0 Configuration
REACT_APP_AUTH0_DOMAIN=your-tenant.auth0.com
REACT_APP_AUTH0_CLIENT_ID=your-client-id
REACT_APP_AUTH0_AUDIENCE=https://your-api-audience
```

**Variable Descriptions:**
- `API_URL`: Base URL for the API (without trailing slash)
- `REACT_APP_AUTH0_DOMAIN`: Your Auth0 domain
- `REACT_APP_AUTH0_CLIENT_ID`: Your Auth0 application client ID
- `REACT_APP_AUTH0_AUDIENCE`: Your Auth0 API audience identifier

### API Environment Variables

The API uses the following environment variables for configuration:

```env
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# PostgreSQL Database Configuration
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DATABASE=StorageLabels
POSTGRES_USERNAME=storage_user
POSTGRES_PASSWORD=your_secure_password
POSTGRES_SSL_MODE=Prefer

# Auth0 Configuration
# Note: Domain, Audience, ClientId, and ApiClientId are configured in appsettings.json
# Only the ClientSecret needs to be set via environment variable
Auth0Settings__ClientSecret=your-client-secret
```

**Variable Descriptions:**
- `POSTGRES_HOST`: PostgreSQL hostname/IP (e.g., `localhost`, `postgres`, `db.example.com`)
- `POSTGRES_PORT`: PostgreSQL port (default: `5432`)
- `POSTGRES_DATABASE`: Database name (default: `StorageLabels`)
- `POSTGRES_USERNAME`: Database user (e.g., `storage_user`)
- `POSTGRES_PASSWORD`: Database password
- `POSTGRES_SSL_MODE`: SSL connection mode (`Disable`, `Prefer`, `Require`) - default: `Prefer`
- `Auth0Settings__ClientSecret`: Your Auth0 client secret (other Auth0 settings are in appsettings.json)

**Note:** As of v2.0.0, the API uses PostgreSQL exclusively. MSSQL support has been removed.

## Local Development

### Build UI Docker Image Locally
```bash
cd storage-labels-ui
docker build -t storage-labels-ui:local .
docker run -p 8080:80 \
  -e API_URL=http://localhost:5000 \
  -e REACT_APP_AUTH0_DOMAIN=your-tenant.auth0.com \
  -e REACT_APP_AUTH0_CLIENT_ID=your-client-id \
  -e REACT_APP_AUTH0_AUDIENCE=https://your-api-audience \
  storage-labels-ui:local
```

### Build API Docker Image Locally
```bash
cd storage-labels-api
docker build -t storage-labels-api:local -f Dockerfile ..
docker run -p 5000:8080 \
  -v $(pwd)/data:/app/data \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e POSTGRES_HOST=localhost \
  -e POSTGRES_PORT=5432 \
  -e POSTGRES_DATABASE=StorageLabels \
  -e POSTGRES_USERNAME=storage_user \
  -e POSTGRES_PASSWORD=your_secure_password \
  -e POSTGRES_SSL_MODE=Prefer \
  -e Auth0Settings__ClientSecret=your-client-secret \
  storage-labels-api:local
```

**Note:** The `-v $(pwd)/data:/app/data` volume mount persists uploaded images between container restarts.

## Docker Compose (Optional)

You can create a `docker-compose.yml` file in the root to run both services:

```yaml
services:
  postgres:
    image: postgres:17-alpine
    environment:
      - POSTGRES_DB=StorageLabels
      - POSTGRES_USER=storage_user
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    ports:
      - '5432:5432'
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U storage_user -d StorageLabels"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    image: {username}/storage-labels-api:latest
    depends_on:
      postgres:
        condition: service_healthy
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - POSTGRES_HOST=postgres
      - POSTGRES_PORT=5432
      - POSTGRES_DATABASE=StorageLabels
      - POSTGRES_USERNAME=storage_user
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_SSL_MODE=Prefer
      - Auth0Settings__ClientSecret=${AUTH0_CLIENT_SECRET}
    volumes:
      - ./data:/app/data

  ui:
    image: {username}/storage-labels-ui:latest
    depends_on:
      - api
    ports:
      - "3000:80"
    environment:
      - API_URL=http://localhost:5000
      - REACT_APP_AUTH0_DOMAIN=your-tenant.auth0.com
      - REACT_APP_AUTH0_CLIENT_ID=your-client-id
      - REACT_APP_AUTH0_AUDIENCE=https://your-api-audience

volumes:
  postgres-data:
```

**Important:** 
- The API uses a volume mount (`./data:/app/data`) to persist uploaded images. This directory will be created on your host machine and contains uploaded image files organized by user.
- PostgreSQL data is persisted in a named volume (`postgres-data`)
- Use environment variables for secrets (see `.env` file example below)

**Version Note:** As of v2.0.0, this application uses PostgreSQL instead of Microsoft SQL Server.

### Using Environment Files

For easier management, create a `.env` file in the root:

**`.env`:**
```env
# UI Environment Variables
API_URL=http://localhost:5000
REACT_APP_AUTH0_DOMAIN=your-tenant.auth0.com
REACT_APP_AUTH0_CLIENT_ID=your-client-id
REACT_APP_AUTH0_AUDIENCE=https://your-api-audience

# Database Credentials
POSTGRES_PASSWORD=your_secure_password

# Auth0 API Secret
AUTH0_CLIENT_SECRET=your-client-secret
```

Docker Compose will automatically load environment variables from the `.env` file.

## Image Tags

The workflows automatically create the following tags:

- `latest` - Latest build from main branch
- `main` - Latest build from main branch
- `main-{sha}` - Specific commit from main branch
- `pr-{number}` - Pull request builds (not pushed to registry)

## Troubleshooting

### Build Failures
- Check the Actions tab in GitHub for detailed logs
- Verify secrets are correctly set
- Ensure Dockerfiles are in the correct locations

### Push Failures
- Verify DOCKER_USERNAME and DOCKER_PASSWORD are correct
- Check that your Docker Hub account has permissions to push
- If using 2FA, make sure you're using an access token, not your password

### Local Build Issues
- Make sure Docker is running
- Check that you're in the correct directory
- Verify all required files are present
