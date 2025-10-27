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

## Local Development

### Build UI Docker Image Locally
```bash
cd storage-labels-ui
docker build -t storage-labels-ui:local .
docker run -p 8080:80 storage-labels-ui:local
```

### Build API Docker Image Locally
```bash
cd storage-labels-api
docker build -t storage-labels-api:local -f Dockerfile ..
docker run -p 5000:8080 storage-labels-api:local
```

## Docker Compose (Optional)

You can create a `docker-compose.yml` file in the root to run both services:

```yaml
version: '3.8'

services:
  ui:
    image: {username}/storage-labels-ui:latest
    ports:
      - "3000:80"
    environment:
      - API_URL=http://api:8080
    depends_on:
      - api

  api:
    image: {username}/storage-labels-api:latest
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=YOUR_DB_CONNECTION_STRING
    volumes:
      - ./data:/app/data
```

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
