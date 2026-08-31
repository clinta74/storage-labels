# storage-labels-android

Native Android client for Storage Labels — Kotlin, Jetpack Compose, Material 3.
Talks to `storage-labels-api`; feature parity target is `storage-labels-ui`.

The full implementation plan, parity checklist and phase breakdown live in
[docs/PLAN.md](docs/PLAN.md).

## Status

Phase 0 (foundations) in progress — project scaffold and containerised build.
Not yet functional: no server setup, auth or data screens.

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

Gradle's caches and `app/build` live in named volumes (`gradle-cache`, `android-build`)
rather than on the bind mount — the Windows↔Linux filesystem boundary otherwise dominates
build time. `docker volume rm storage-labels-android_gradle-cache` for a clean slate.

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
