# storage-labels-android

Native Android client for Storage Labels — Kotlin, Jetpack Compose, Material 3.
Talks to `storage-labels-api`; feature parity target is `storage-labels-ui`.

The full implementation plan, parity checklist and phase breakdown live in
[docs/PLAN.md](docs/PLAN.md).

## Status

All five build phases are written and compiling: setup and auth, locations/boxes/items,
images and search with QR scanning, label runs with Avery 94107 printing, and the admin
surfaces (common locations, users, encryption keys with live rotation progress).

**Nothing here has run on a device or emulator yet.** It compiles, lints and passes 52 unit
tests, but no line of it has executed against a real server. Treat the whole thing as
unverified until someone installs it and signs in. See docs/PLAN.md for what to check
first.

## Building

### With Docker (no local SDK needed)

```bash
docker compose build android                              # one-time, ~5 min
docker compose run --rm android ./gradlew assembleDebug   # APK in app/build/outputs/apk/debug
docker compose run --rm android ./gradlew test            # unit tests
docker compose run --rm android ./gradlew lintDebug       # lint
```

`app/build` lives in a volume, so copy the APK out to the host before installing:

```bash
docker compose run --rm android cp app/build/outputs/apk/debug/app-debug.apk outputs/
```

Gradle's caches, `app/build` and `.gradle` live in named volumes rather than on the bind
mount. That is partly speed — the Windows↔Linux filesystem boundary otherwise dominates
build time — and partly correctness: Gradle memory-maps files under `.gradle`, which fails
intermittently with `java.io.IOException: Input/output error` over the mount.
`./gradlew clean` fails against this layout — Gradle can't delete a mounted directory.
For a clean slate, drop the volumes instead:

```bash
docker volume rm storage-labels-android_android-build                  storage-labels-android_gradle-project-cache                  storage-labels-android_gradle-cache
```

### With a local SDK

Install JDK 21 and Android Studio (which brings the SDK), then `./gradlew assembleDebug`.
The wrapper supplies Gradle itself, so nothing else needs installing.

## Installing on a device

The container cannot see USB devices, so install from the host with `adb`
(`winget install Google.PlatformTools` on Windows):

```bash
adb install -r outputs/app-debug.apk
```

A physical device matters for this app: camera capture, torch, QR scanning and
label printing can't be validated on an emulator. The emulator is not available
inside the container at all — it needs `/dev/kvm`, which Docker Desktop and
Rancher Desktop do not expose on Windows. Run it natively via Android Studio if
you want one.

## Releasing

Versions come from git tags, as they do for the UI and API — see
[VERSIONING.md](../VERSIONING.md). The app's tags are prefixed `android-v`:

```bash
git tag android-v0.2.0
git push origin android-v0.2.0
```

That runs [release-android.yml](../.github/workflows/release-android.yml), which builds a
signed APK and AAB and attaches them to a GitHub Release. Unlike the UI and API, nothing
publishes automatically on a push to main: an installed app version is user-visible, so
bumping it is a deliberate act. Pushes and PRs still build, test and lint through
[build-android.yml](../.github/workflows/build-android.yml).

`versionName` is the tag; `versionCode` is derived from it as
`major * 10000 + minor * 100 + patch` (`0.2.0` → `200`). Android requires that number to
increase with every build a device might install over the last, and never to repeat, so it
is computed from the version rather than from a build counter — the APK's version is
readable straight out of its code. The scheme holds only while minor and patch stay under
100; the workflow fails the build rather than publishing a version that would break it.

Both values default to the ones in [app/build.gradle.kts](app/build.gradle.kts) and are
overridden by `-PappVersionName` / `-PappVersionCode`, so local builds need no setup.

### Signing key

An unsigned APK will not install, so a tagged release needs a key. It lives in repository
secrets, never in the repo. To create one:

```bash
keytool -genkeypair -v -keystore storage-labels-release.jks \
  -alias storage-labels -keyalg RSA -keysize 2048 -validity 10000
base64 -w0 storage-labels-release.jks    # certutil -encode on Windows
```

Keep the `.jks` file and its passwords somewhere safe — losing them means no future build
can update an installed app, since Android identifies an app by its signature. Then add
four repository secrets:

| Secret | Value |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | the base64 above, one line |
| `ANDROID_KEYSTORE_PASSWORD` | keystore password |
| `ANDROID_KEY_ALIAS` | `storage-labels` |
| `ANDROID_KEY_PASSWORD` | key password (the same one unless you set it apart) |

Until those exist, a tag push fails with an explicit message, and the workflow's manual
"Run workflow" button produces a debug-signed APK instead — installable for testing, under
the `.debug` application id.

Building a signed release locally works the same way, through the environment:

```bash
docker compose run --rm \
  -e ANDROID_KEYSTORE_FILE=/workspace/storage-labels-release.jks \
  -e ANDROID_KEYSTORE_PASSWORD=... -e ANDROID_KEY_ALIAS=storage-labels \
  -e ANDROID_KEY_PASSWORD=... \
  android ./gradlew assembleRelease -PappVersionName=0.2.0 -PappVersionCode=200
```

With those variables unset the build still succeeds; AGP just names the output
`app-release-unsigned.apk`, and Android will refuse to install it. `*.jks` is gitignored.

## Toolchain

| | |
|---|---|
| Gradle | 9.7.1 (wrapper) |
| AGP | 9.3.2 |
| Kotlin | 2.4.10 |
| JDK | 21 (targets Java 17 bytecode) |
| compileSdk / targetSdk | 37 |
| minSdk | 26 |

Versions are pinned in [gradle/libs.versions.toml](gradle/libs.versions.toml).
