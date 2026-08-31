# storage-labels-android

Native Android client for Storage Labels — Kotlin, Jetpack Compose, Material 3.
Talks to `storage-labels-api`; feature parity target is `storage-labels-ui`.

The full implementation plan, parity checklist and phase breakdown live in
[docs/PLAN.md](docs/PLAN.md).

## Status

Phases 0–1 in progress. Working end to end: server setup, sign in, session restore,
token refresh, sign out, and no-auth mode. Not yet built: registration, change password,
the app shell (drawer, preferences, legal pages) and every data screen.

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
`docker volume rm storage-labels-android_gradle-cache` for a clean slate.

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
