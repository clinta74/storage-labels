# Docker Image Versioning Strategy

## Auto-Incrementing Version Format

This project uses an automatic versioning system for Docker images:

**Format:** `YYYY.M.BUILD_NUMBER`

- `YYYY` - Current year (e.g., 2025)
- `M` - Current month without leading zero (e.g., 1, 10, 12)
- `BUILD_NUMBER` - GitHub Actions run number (auto-increments)

**Examples:**
- `2025.10.42` - 42nd build in October 2025
- `2025.11.1` - 1st build in November 2025
- `2025.12.150` - 150th build in December 2025

## How It Works

### Automatic Versioning
Every time GitHub Actions runs for the UI or API:
1. A version number is generated using the current date and GitHub run number
2. The Docker image is built with this version
3. The image is tagged with:
   - The version number (e.g., `2025.10.42`)
   - `latest` (for main branch builds) - Most recent build
   - `stable` (for main branch builds) - Same as latest, indicates production-ready
   - Git commit SHA (e.g., `sha-abc1234`)

### Tag Meanings

| Tag | Purpose | Updates | Use When |
|-----|---------|---------|----------|
| `2025.10.42` | Specific version | Never changes | You need a specific build |
| `latest` | Most recent build from main | Every main branch build | You want the newest version |
| `stable` | Production-ready version | Every main branch build | You want a stable version |
| `sha-abc1234` | Specific git commit | Never changes | You need exact commit version |
| `pr-123` | Pull request build | Each PR build | Testing PRs before merge |

**Note:** In this setup, `latest` and `stable` are the same - both point to the most recent main branch build. All main branch builds are considered production-ready.

### GitHub Actions
The workflows automatically:
- Generate version numbers on every build
- Pass the version to Docker as a build argument
- Tag images with multiple tags for flexibility
- Push to Docker Hub (except for pull requests)

### Version Labels
Each Docker image includes metadata labels:
```dockerfile
LABEL version="2025.10.42"
LABEL org.opencontainers.image.version="2025.10.42"
```

You can inspect the version with:
```bash
docker inspect storage-labels-ui:latest | grep version
```

## Usage

### Pulling Images

**Production/Stable (recommended for deployments):**
```bash
docker pull username/storage-labels-ui:stable
docker pull username/storage-labels-api:stable
```

**Latest build (same as stable):**
```bash
docker pull username/storage-labels-ui:latest
docker pull username/storage-labels-api:latest
```

**Specific version (for rollbacks or pinning):**
```bash
docker pull username/storage-labels-ui:2025.10.42
docker pull username/storage-labels-api:2025.10.42
```

**Specific commit (for debugging):**
```bash
docker pull username/storage-labels-ui:sha-abc1234
```

### Docker Compose Example

**Pin to stable:**
```yaml
services:
  ui:
    image: username/storage-labels-ui:stable
  api:
    image: username/storage-labels-api:stable
```

**Pin to specific version:**
```yaml
services:
  ui:
    image: username/storage-labels-ui:2025.10.42
  api:
    image: username/storage-labels-api:2025.10.42
```

### Local Development

When building locally without GitHub Actions, images will be tagged as `dev`:
```bash
docker build -t storage-labels-ui:dev ./storage-labels-ui
docker build -t storage-labels-api:dev ./storage-labels-api
```

To build with a custom version:
```bash
docker build --build-arg VERSION=1.0.0 -t storage-labels-ui:1.0.0 ./storage-labels-ui
docker build --build-arg VERSION=1.0.0 -t storage-labels-api:1.0.0 ./storage-labels-api
```

## Version History

GitHub Actions maintains the complete history of builds. You can:
- View all tags on Docker Hub
- Check the Actions tab for build numbers
- Track versions through Git commits

## Benefits

✅ **Automatic** - No manual version bumping required
✅ **Chronological** - Easy to understand when a build was created
✅ **Unique** - Every build gets a unique version number
✅ **Traceable** - Can track back to exact GitHub Actions run
✅ **Semantic** - Year and month provide context
