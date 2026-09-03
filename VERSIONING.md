# Docker Image Versioning Strategy

## Auto-Incrementing Semantic Versioning

This project uses automatic semantic versioning for Docker images:

**Format:** `MAJOR.MINOR.PATCH` (e.g., `1.2.3`)

- `MAJOR` - Major version (breaking changes)
- `MINOR` - Minor version (new features, backward compatible)
- `PATCH` - Patch version (bug fixes, auto-increments on every build)

**Examples:**
- `0.0.1` - First build
- `0.0.42` - 42nd patch build
- `1.0.0` - First major release
- `1.2.15` - 15th patch of version 1.2

## Git Tags

### UI Tags
- Format: `ui-v1.2.3`
- Example: `ui-v0.0.1`, `ui-v0.0.2`, `ui-v1.0.0`
- Tags before `ui-v5.0.0` used a bare `v1.2.3` and are kept for history only

### API Tags
- Format: `api-v1.2.3`
- Example: `api-v0.0.1`, `api-v0.0.2`, `api-v1.0.0`

### Android Tags
- Format: `android-v1.2.3`
- Example: `android-v0.2.0`, `android-v1.0.0`

This allows independent versioning of the UI, API and Android app.

## How It Works

### Automatic Versioning
Every time GitHub Actions runs for the UI or API:
1. Finds the latest Git tag (e.g., `v0.0.5` or `api-v0.0.5`)
2. Auto-increments the PATCH version (e.g., `v0.0.6`)
3. Creates a new Git tag
4. Builds the Docker image with this version
5. Tags the Docker image with:
   - The version number (e.g., `0.0.6`)
   - The Git tag (e.g., `v0.0.6`)
   - `latest` (for main branch builds)
   - `stable` (for main branch builds)
   - Git commit SHA (e.g., `sha-abc1234`)
6. Creates a GitHub Release

### The Android App Is Different

The app does not auto-increment, because a version installed on someone's phone is
user-visible in a way a Docker tag is not. Nothing is published until you push a tag:

```bash
git tag android-v0.2.0
git push origin android-v0.2.0
```

That builds a signed APK and AAB and attaches them to a GitHub Release. Pushes to main
still run the app's tests, lint and a debug build — they just don't publish.

Android also needs a second number. `versionName` is the tag (`0.2.0`); `versionCode` is
an integer derived from it as `major * 10000 + minor * 100 + patch` (`0.2.0` → `200`).
Android requires that number to increase with every build a device installs over the last
and never to repeat, so it is computed from the version rather than a build counter, which
keeps an APK's version readable straight out of its code. Minor and patch must stay below
100 or the arithmetic stops being monotonic; the workflow fails the build if they don't.

Publishing needs a signing key in repository secrets — see
[storage-labels-android/README.md](storage-labels-android/README.md#signing-key). Without
it a tag push fails with an explicit message rather than publishing an APK that cannot be
installed.

### Tag Meanings

| Tag | Purpose | Updates | Use When |
|-----|---------|---------|----------|
| `0.0.6` | Specific version | Never changes | You need a specific build |
| `v0.0.6` | Git tag version | Never changes | Same as above, with v prefix |
| `latest` | Most recent build from main | Every main branch build | You want the newest version |
| `stable` | Production-ready version | Every main branch build | You want a stable version for TrueNAS |
| `sha-abc1234` | Specific git commit | Never changes | You need exact commit version |
| `pr-123` | Pull request build | Each PR build | Testing PRs before merge |

**Note:** `latest` and `stable` are the same - both point to the most recent main branch build.

### Manual Version Bumps

**To bump MINOR version (new features):**
```bash
git tag v0.1.0
git push origin v0.1.0
# Next auto build will be v0.1.1
```

**To bump MAJOR version (breaking changes):**
```bash
git tag v1.0.0
git push origin v1.0.0
# Next auto build will be v1.0.1
```

**For API:**
```bash
git tag api-v1.0.0
git push origin api-v1.0.0
# Next auto build will be api-v1.0.1
```

**For Android** (every version is a manual bump — there is no auto build):
```bash
git tag android-v1.0.0
git push origin android-v1.0.0
```

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
docker pull username/storage-labels-ui:0.0.42
docker pull username/storage-labels-api:0.0.42
```

**With v prefix:**
```bash
docker pull username/storage-labels-ui:v0.0.42
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
    image: username/storage-labels-ui:0.0.42
  api:
    image: username/storage-labels-api:0.0.42
```

## TrueNAS Configuration

### For TrueNAS Custom Apps

TrueNAS can track updates using semantic versioning:

1. **Use `stable` tag for auto-updates:**
   - Image: `username/storage-labels-ui:stable`
   - TrueNAS will detect when the `stable` tag points to a new version

2. **Use specific version for pinning:**
   - Image: `username/storage-labels-ui:0.0.42`
   - Won't auto-update, requires manual version change

3. **Check for updates:**
   - TrueNAS polls Docker Hub for new tags
   - When a new version is pushed, TrueNAS will show an update is available
   - GitHub Releases also help track version history

### Version Detection
- Each build creates a Git tag and GitHub Release
- Docker images are tagged with semantic versions
- TrueNAS can detect version changes by comparing tags
- The `stable` tag always points to the latest production build

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

### Tracking Versions
- **Git Tags**: View all tags with `git tag -l`
- **GitHub Releases**: Check the Releases page for version history with auto-generated notes
- **Docker Hub**: Browse all available image tags
- **GitHub Actions**: View build history in the Actions tab

### Example Version Progression
```
v0.0.1  → First build
v0.0.2  → Second build (auto-increment)
v0.0.3  → Third build (auto-increment)
v0.1.0  → Manual bump to add new feature
v0.1.1  → Auto-increment continues
v1.0.0  → Manual bump for major release
v1.0.1  → Auto-increment continues
```

## Benefits

✅ **Automatic** - PATCH version auto-increments on every build
✅ **Semantic** - Follows semver standard (MAJOR.MINOR.PATCH)
✅ **TrueNAS Compatible** - Uses proper versioning that TrueNAS can detect
✅ **Traceable** - Git tags, GitHub Releases, and Docker tags all synchronized
✅ **Flexible** - Can manually bump MAJOR/MINOR versions when needed
✅ **Standard** - Follows industry-standard semantic versioning
✅ **Unique** - Every build gets a unique, incrementing version number
