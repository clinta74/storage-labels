# Native Android App — Implementation Plan

**Goal:** a native Kotlin/Jetpack Compose Android client (`storage-labels-android`) with full feature parity with `storage-labels-ui` (v7.0.0), talking to the existing `storage-labels-api` with no required backend rewrite.

**Status:** Phases 0-5 are written, compiling, linting clean, and covered by 52 unit tests. Every feature in the parity checklist below has an implementation.

**None of it has run on a device or emulator.** There was no hardware attached while it was built, so the entire app is unverified at runtime. The riskiest parts, in the order worth checking: sign-in and token refresh against a real server (the refresh cookie needs HTTPS), the Keystore encryption on first launch, camera permission and QR scanning, and the printed label alignment against a physical Avery 94107 sheet — which no test can confirm.

See [../README.md](../README.md) to build and install it.

---

## 1. What the web UI actually does (parity checklist)

Derived from `storage-labels-ui/src`. Every row below must exist in the Android app to claim parity.

| # | Web route / component | Feature | API calls |
|---|---|---|---|
| 1 | `/login` `auth/login.tsx` | Username-or-email + password login | `POST /api/auth/login` |
| 2 | `/register` `auth/register.tsx` | Self-registration (email, username, password, first/last), auto-login after | `POST /api/auth/register` |
| 3 | bootstrap `auth-provider.tsx` | Read auth mode (`Local` \| `None`); in `None` mode skip login entirely and show the "no auth" warning banner | `GET /api/auth/config`, `GET /api/auth/me` |
| 4 | `api-provider.tsx` | Silent token refresh on 401 + single-flight retry, session-expired → back to login | `POST /api/auth/refresh`, `POST /api/auth/logout` |
| 5 | `/locations` `locations.tsx` | List locations with access level | `GET /api/location` |
| 6 | `/locations/add`, `/:id/edit` | Create / rename location | `POST /api/location`, `PUT /api/location/{id}` |
| 7 | `/locations/:id` `location.tsx` | Boxes in a location, per-box item-count badges, delete box, delete location (with **force** checkbox), settings menu | `GET /api/box/location/{id}`, `GET /api/item/box/{id}`, `DELETE /api/box/{id}?force=`, `DELETE /api/location/{id}?force=` |
| 8 | `/locations/:id/users` `manage-location-users.tsx` | Share a location: add user by email with access level, change level, remove; "user not found" dialog | `GET/POST/PUT/DELETE /api/location/{id}/users[/{userId}]` |
| 9 | `/locations/:id/box/add`, `box/edit` | Create/edit box: code (with inline QR scanner), name, description, image | `POST /api/box`, `PUT /api/box/{id}` |
| 10 | `/locations/:id/box/:boxId` `box.tsx` (670 LOC — biggest screen) | Box detail: image, color-formatted code, item list, item detail modal, add/edit/delete item, **move box** to another location, delete box (force), inline global search + QR scan | `GET /api/box/{id}`, `GET /api/item/box/{id}`, `PUT /api/box/{id}/move`, `DELETE /api/item/{id}` |
| 11 | `box/item/add`, `item/:itemId/edit` | Item create/edit with image selector | `POST /api/item`, `PUT /api/item/{id}` |
| 12 | `shared/search-bar.tsx` + `search-results.tsx` | Global search as-you-type, 10/page infinite scroll, relevance rank, result → navigate to box; QR scan → direct box lookup | `GET /api/search?query&pageNumber&pageSize` (total in `X-Total-Count` header), `GET /api/search/qrcode/{code}` |
| 13 | `/images` `images.tsx`, `image-card.tsx` | Image gallery: upload (camera or file), size/date/reference counts, delete (force when referenced) | `GET /api/images`, `POST /api/images` (multipart), `DELETE /api/images/{id}[/force]` |
| 14 | `shared/image-capture.tsx` (408 LOC) | Camera capture UI: front/back switch, torch, gallery pick, preview + confirm | — |
| 15 | `shared/image-selector.tsx` | Pick an existing image or upload a new one, used by box/item forms | `GET /api/images`, `POST /api/images` |
| 16 | `shared/authenticated-image.tsx` | Fetch images with `Authorization` header, honor the `showImages` preference, placeholder + error states | `GET /api/images/{imageId}` |
| 17 | `/labels` `label-jobs.tsx` | Label print jobs list | `GET /api/labels` |
| 18 | `/labels/create`, `/:jobId/edit` | Job config: name, format (`Avery94107`), algorithm (`NumericOnly` \| `Base36Suffix`), prefix, suffix length, start index, code color pattern | `POST /api/labels`, `PUT /api/labels/{id}` |
| 19 | `/labels/:jobId` `label-job.tsx` | Job detail + stats (last index, total generated), delete, "Print Next Page" | `GET /api/labels/{id}`, `POST /api/labels/{id}/next-page`, `DELETE /api/labels/{id}` |
| 20 | `/labels/:jobId/print` `label-print-page.tsx` + `label-item.tsx` | Avery 94107 sheet: 3×4 grid of 2″ cells, 0.5″ top / 0.875″ left margin, 0.375″ col gap, 0.5″ row gap, QR + color-formatted monospace code, browser print | — (renders the page returned by `next-page`) |
| 21 | `/common-locations` | Common location list / add / delete — gated on `read:common-locations` / `write:common-locations` | `GET/POST/DELETE /api/common-location` |
| 22 | `/encryption-keys` (503 LOC) | Key list, create, activate (with `autoRotate`), retire, per-key stats — gated on `*:encryption-keys` | `GET/POST /api/admin/encryption-keys`, `PUT .../{kid}/activate?autoRotate=`, `PUT .../{kid}/retire`, `GET .../{kid}/stats` |
| 23 | `/encryption-keys/rotations` (534 LOC) | Rotation history, start manual rotation, live progress via **SSE**, cancel | `POST .../rotate`, `GET .../rotations[/{id}]`, `GET .../rotations/{id}/stream` (SSE), `DELETE .../rotations/{id}` |
| 24 | `/users` `user-management.tsx` | User list with roles, change role, admin password reset, delete user — gated on `write:user` | `GET /api/user/all`, `PUT /api/user/{id}/role`, `POST /api/auth/admin/reset-password`, `DELETE /api/user/{id}` |
| 25 | `/preferences` | Theme (light/dark), `showImages`, `codeColorPattern` | `GET/PUT /api/user/preferences` |
| 26 | `/change-password` | Self-service password change | `POST /api/auth/change-password` |
| 27 | `/legal/privacy`, `/legal/terms` | Static legal pages (public, pre-auth) | — |
| 28 | `shared/formatted-code.tsx`, `label-item.tsx` | Code color pattern parser (`3:primary,2:secondary,*,4:error`) — duplicated in the web app; port **once** in Kotlin | — |
| 29 | `providers/alert-provider`, `snackbar-provider` | Global error alerts + success snackbars | — |
| 30 | `shared/breadcrumbs`, `empty-state`, `footer` | Navigation affordances, empty states | — |

**Roles/permissions** (`Models/Authorization.cs`, JWT `permission` claims, one claim per permission):
`write:user`, `read:user`, `write:common-locations`, `read:common-locations`, `write:encryption-keys`, `read:encryption-keys`.
Roles: **Admin** (all), **Auditor** (read-only), **User** (none of the above). In `None` auth mode the server grants every permission and the client must treat `hasPermission` as always true.

---

## 2. API contract notes that shape the client

These are the things that will bite if they're not designed for up front.

1. **Refresh token is an HttpOnly cookie** (`CookieHelpers.cs`): `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`. `POST /api/auth/refresh` reads it from `Request.Cookies` only — there is no request-body variant.
   - **Chosen approach:** implement a persistent OkHttp `CookieJar` backed by encrypted storage, scoped to the configured host. `SameSite` is a browser concept and is ignored by OkHttp; `Secure` means **the refresh flow only works over HTTPS**.
   - **Recommended small API change (not blocking):** accept an optional `{ "refreshToken": "..." }` body on `/api/auth/refresh` and return the refresh token in the login/refresh JSON when a `X-Client: native` header is present. This removes cookie-jar fragility and makes plain-HTTP LAN deployments work. Add it in Phase 1 as an API-side task; the client should be written against an interface so either path is a one-line swap.
2. **Access token** is a JWT in `Authorization: Bearer`. The client mirrors the web behaviour: single-flight refresh on 401, retry once, and hard logout on refresh failure. `/api/auth/*` responses must never trigger the refresh interceptor (same guard as `api-provider.tsx`).
3. **Permissions come from the JWT**, not from a permissions endpoint — decode the `permission` claims (the web app uses `jwt-decode`). Use a small local JWT payload decoder; do not pull a full JOSE library just for this.
4. **Server URL is per-install.** This is a self-hosted product (Docker Compose, TrueNAS, home lab). The app needs a first-run "Server address" screen, validated by calling `GET /api/auth/config`, stored in DataStore. Ship `usesCleartextTraffic=false` by default with an opt-in "allow insecure HTTP" toggle wired to a `network_security_config.xml` domain exception — and warn that refresh-cookie auth requires HTTPS.
5. **Images are JPEG-only** and access-controlled: `GET /api/images/{imageId}` requires the bearer token, so Coil needs an authenticated fetcher (or an OkHttp client with the auth interceptor). Upload is `multipart/form-data` with the field name `file`.
   - **Contract mismatch to fix:** the web `Api.Image.uploadImage` is typed `post<string>` but `UploadImage.cs` returns the full `ImageMetadata` object. Model the Android DTO off the **server** (`ImageMetadata`), not the TS typing.
6. **Search pagination** puts the total in the `X-Total-Count` response header — the Retrofit call must return `Response<List<SearchResultResponse>>` to read it.
7. **Rotation progress is SSE** (`text/event-stream`, `data: {json}` lines, terminating when `status != InProgress`). OkHttp handles this fine with a streaming response body read line-by-line off the main thread; no extra dependency needed.
8. **Rate limiting is enabled** server-side (`RateLimitingExtensions.cs`). Handle `429` centrally with a retry-after-aware error surface rather than per-screen.
9. **Force-delete semantics** exist on locations, boxes and images (`?force=true`, `/force`). The confirmation UX (checkbox before enabling destructive confirm) must be carried over.
10. **Label codes are allocated server-side and are stateful** — `POST /api/labels/{id}/next-page` *advances* `lastGeneratedIndex`. Never call it speculatively (e.g. on screen resume, or as a retry after an ambiguous failure); allocate only on explicit user action and cache the returned page in the ViewModel so rotation/back-navigation doesn't burn a page.

---

## 3. Target architecture

**Stack**

| Concern | Choice | Notes |
|---|---|---|
| Language / build | Kotlin 2.4.10, Gradle 9.7.1, AGP 9.3.2, version catalog (`libs.versions.toml`) | AGP 9 supplies Kotlin itself — applying `org.jetbrains.kotlin.android` is an error |
| Min / target SDK | min 26, target 37 | min 26 covers >95% of devices and unlocks `java.time`, adaptive icons. compileSdk 37 is a hard floor: current AndroidX and OkHttp refuse to compile against 36 |
| UI | Jetpack Compose + Material 3 | Maps cleanly onto the MUI-based web UI |
| Navigation | Navigation Compose, type-safe routes (`@Serializable` route objects) | Mirrors the nested React Router tree |
| DI | Hilt | |
| Networking | Retrofit + OkHttp + kotlinx.serialization | Interceptors for auth, refresh, server URL |
| Persistence | DataStore (settings, server URL, tokens via `EncryptedSharedPreferences`/Keystore); Room only if offline mode is taken up | |
| Async | Coroutines + Flow; `StateFlow` UI state per screen | |
| Images | Coil 3 with an OkHttp loader sharing the auth interceptor | |
| Camera | CameraX (`ImageCapture`, torch, lens switch) | Replaces `image-capture.tsx` |
| QR scan | ML Kit Barcode Scanning (bundled model) + CameraX `ImageAnalysis` | Replaces `@yudiel/react-qr-scanner` |
| QR generate | ZXing `core` (`QRCodeWriter`) → `Bitmap` | Replaces `qrcode.react` |
| Printing | Android `PrintManager` + custom `PrintDocumentAdapter` drawing a Letter `PdfDocument` | Replaces browser `window.print()` |
| Background upload | WorkManager (optional, Phase 5) | Retryable image upload |
| Testing | JUnit5 + Turbine + MockWebServer; Compose UI tests; Paparazzi/Roborazzi for the label sheet | |

**Module layout** (single Gradle project, feature packages; split into modules only if build times demand it)

```
storage-labels-android/
  app/
    src/main/kotlin/net/pollyspeople/storagelabels/
      StorageLabelsApp.kt            // @HiltAndroidApp
      MainActivity.kt                // single activity, Compose host
      core/
        network/                     // Retrofit, OkHttp, interceptors, CookieJar, SSE client
        auth/                        // AuthRepository, TokenStore, JwtClaims, AuthMode, session state
        permissions/                 // Permissions constants, LocalPermissions CompositionLocal
        ui/                          // theme (light/dark), StorageLabelsScaffold, EmptyState,
                                     //   Breadcrumbs, SnackbarHost, ErrorAlert, ConfirmDialog
        code/                        // CodeColorPattern parser + FormattedCode composable
        result/                      // ApiResult<T>, error mapping (401/403/429/validation problem)
      data/
        api/                         // Retrofit service interfaces (one per web endpoint file)
        dto/                         // @Serializable mirrors of Models/DTO
        repository/                  // LocationRepo, BoxRepo, ItemRepo, ImageRepo, LabelRepo,
                                     //   SearchRepo, UserRepo, CommonLocationRepo, EncryptionKeyRepo
      feature/
        server/      // first-run server URL setup
        auth/        // login, register, change password
        locations/   // list, add, edit, detail, users
        boxes/       // add, edit, detail
        items/       // add, edit, detail sheet
        search/      // search bar + results + QR scan
        images/      // gallery, capture, selector
        labels/      // jobs, create, edit, detail, print (PDF/print adapter)
        commonlocations/
        encryptionkeys/  // keys, rotations (SSE)
        users/       // user management
        preferences/
        legal/
      navigation/    // NavHost graph, type-safe routes, deep links
```

**Navigation map** (web route → Android destination)

```
/login                                  -> Auth.Login
/register                               -> Auth.Register
(first run / no server configured)      -> Server.Setup
/locations                              -> Locations.List            (start destination)
/locations/add|:id/edit                 -> Locations.Edit(id?)
/locations/:id                          -> Locations.Detail(id)
/locations/:id/users                    -> Locations.Users(id)
/locations/:id/box/add|:boxId/edit      -> Boxes.Edit(locationId, boxId?)
/locations/:id/box/:boxId               -> Boxes.Detail(locationId, boxId)
/…/box/:boxId/item/add|:itemId/edit     -> Items.Edit(boxId, itemId?)
/images                                 -> Images.Gallery
/labels                                 -> Labels.List
/labels/create|:jobId/edit              -> Labels.Edit(jobId?)
/labels/:jobId                          -> Labels.Detail(jobId)
/labels/:jobId/print                    -> Labels.Print(jobId)       (page passed via SavedState)
/common-locations                       -> CommonLocations.List
/encryption-keys                        -> EncryptionKeys.List
/encryption-keys/rotations              -> EncryptionKeys.Rotations
/users                                  -> Users.Management
/preferences                            -> Preferences
/change-password                        -> Auth.ChangePassword
/legal/privacy|/legal/terms             -> Legal.Privacy | Legal.Terms
```

The nav drawer / bottom bar replaces `navigation-bar.tsx`, with Common Locations, Encryption Keys and Users hidden behind the same permission checks. Breadcrumbs become the top app bar title + back stack.

---

## 4. Android-native replacements for web-only mechanics

| Web mechanism | Android replacement |
|---|---|
| `window.print()` + CSS `@page` | `PrintManager.print()` with a `PrintDocumentAdapter` that renders a Letter (8.5″×11″ @ 72pt) `PdfDocument` page: 0.5″ top / 0.875″ left margin, 3 cols × 4 rows of 2″ cells, 0.375″ column gap, 0.5″ row gap, ZXing QR at ~1.67″, monospace code below with per-segment colors. Also expose "Save/Share PDF" via `FileProvider`. |
| `getUserMedia` camera dialog | CameraX preview + `ImageCapture`, `CameraControl.enableTorch`, front/back `CameraSelector`, `PhotoPicker` for gallery, plus a preview/confirm step |
| `@yudiel/react-qr-scanner` | CameraX `ImageAnalysis` + ML Kit `BarcodeScanning` (QR format only), debounced first-hit |
| `localStorage` token | `EncryptedSharedPreferences` (Keystore-backed) for access token; cookie jar for the refresh cookie |
| Browser cookie jar | Persistent OkHttp `CookieJar` (host-scoped, encrypted at rest) |
| React Context providers | Hilt-scoped repositories + `CompositionLocal` for user/permissions/theme |
| MUI theme from `preferences.theme` | Material 3 color scheme driven by the same server-side preference; offer "follow system" as an added third option, still persisting light/dark to the API |
| Infinite scroll in search | `LazyColumn` + `snapshotFlow` on last visible index, or Paging 3 if it earns its keep |
| SSE via `fetch` reader | OkHttp streaming response, line reader in a `flow { }` on `Dispatchers.IO` |

**Worth adding because it's native (post-parity, Phase 6):** app-wide QR scan shortcut (launcher/quick-settings tile), share-sheet import of a photo into a box, offline read cache, biometric app lock, home-screen widget for "scan a box".

---

## 5. Phased delivery

Estimates assume one developer working steadily; they're relative sizing, not a commitment.

### Phase 0 — Foundations (≈1 week)
- New Gradle project + version catalog + Hilt + Compose scaffolding, CI workflow (`.github/workflows/build-android.yml`) mirroring the existing UI/API workflows: assemble debug, ktlint/detekt, unit tests.
- `core/network`: Retrofit + kotlinx.serialization, dynamic base URL from DataStore, auth interceptor, single-flight refresh authenticator, persistent cookie jar, `ApiResult` error mapping (401/403/404/429/`ValidationProblem`).
- Server setup screen + `GET /api/auth/config` probe; cleartext opt-in.
- **Exit criteria:** app launches, points at a server, reads auth config, shows Login or goes straight to the app in `None` mode.

### Phase 1 — Auth & shell (≈1 week)
- Login, Register, session bootstrap (`/api/auth/me`), logout, change password, session-expired handling.
- JWT permission decoding + `LocalPermissions`; role-gated nav.
- App shell: navigation drawer/bottom bar, snackbar + error-alert host, theme (light/dark from preferences), Preferences screen, legal screens, "No authentication mode" warning banner.
- *(API side, optional but recommended)* body-based refresh token for native clients.
- **Exit criteria:** log in on a real device against a real server, token refresh survives a 60-minute expiry, preferences round-trip.

### Phase 2 — Core inventory (≈2 weeks)
- Locations: list, add, edit, delete (+force), access-level display.
- Location detail: boxes, item counts, box delete.
- Box detail: full parity including move-box, delete (+force), item list, item detail sheet.
- Items: add/edit/delete.
- `CodeColorPattern` parser + `FormattedCode` composable (unit-tested against the web parser's behaviour, including the `*` skip segment and malformed-pattern fallback).
- Manage location users (add by email, change level, remove, not-found handling).
- **Exit criteria:** a user can do everything the web app does with locations/boxes/items except images and search.

### Phase 3 — Images & search (≈1.5 weeks)
- Coil authenticated image loading, `showImages` preference honored, placeholder/error states.
- Image gallery: list, upload, reference counts, delete (+force).
- CameraX capture screen (torch, lens flip, gallery pick, preview/confirm) + image selector sheet used by box/item forms.
- Global search: as-you-type, `X-Total-Count` paging, infinite scroll, relevance display, result → box.
- ML Kit QR scanning: from the search bar and from the box-code field; `search/qrcode/{code}` → navigate to box.
- **Exit criteria:** scan a physical label and land on the right box; capture a photo and attach it to a box.

### Phase 4 — Labels & printing (≈1.5 weeks)
- Job list / create / edit / detail / delete with the full config surface.
- "Print Next Page" with the anti-double-allocation guard, page cached in `SavedStateHandle`.
- Avery 94107 `PrintDocumentAdapter` + preview composable that matches the printed geometry; screenshot test against a golden PDF/bitmap so margins can't silently regress.
- Share/save PDF.
- **Exit criteria:** printed sheet aligns with real Avery 94107 stock (verify on paper against a physical sheet — this is the one thing no test can confirm).

### Phase 5 — Admin surfaces (≈1.5 weeks)
- Common locations (permission-gated).
- User management: list, role change, admin reset password, delete.
- Encryption keys: list, create, activate (autoRotate), retire, stats.
- Rotations: history, start manual rotation, **SSE live progress**, cancel.
- **Exit criteria:** an Admin can run a full key rotation from the phone and watch it progress live.

### Phase 6 — Polish & release (≈1 week)
- Empty states, loading skeletons, error copy, accessibility (content descriptions, touch targets, TalkBack pass), dark theme audit, tablet/landscape layouts.
- Play Store assets or self-hosted APK/AAB signing in CI; versioning aligned with `VERSIONING.md`.
- README + `docs/android-app/` setup notes.

**Total: ≈9–10 weeks** for full parity, or ≈6 weeks to a genuinely useful daily driver (Phases 0–3, which covers scan → find → edit, the actual point of the product).

---

## 6. Testing strategy

- **Unit:** `CodeColorPattern` parser (port the web cases plus malformed input), base36 code preview logic, JWT claim decoding, `ApiResult` error mapping, refresh single-flight (concurrent 401s must trigger exactly one refresh).
- **Network:** MockWebServer per repository — happy path, 401→refresh→retry, 401→refresh-fails→logout, 429, `X-Total-Count` parsing, SSE stream parsing/termination, multipart upload shape.
- **UI:** Compose tests for login, box detail, search paging, destructive-confirm flows (force checkbox gating).
- **Screenshot:** Roborazzi/Paparazzi golden for the Avery 94107 sheet and for light/dark theming.
- **Manual:** printed-sheet alignment; camera/torch/lens on at least two physical devices; `None` auth mode against a LAN server.

## 7. Open decisions

1. **Refresh-token transport** — persistent cookie jar (no API change, HTTPS-only) vs. adding a body/JSON refresh for native clients (small API change, works on plain-HTTP LAN installs). Recommendation: build the client behind an interface, and add the API variant in Phase 1.
2. **Offline support** — not in the web app, so out of parity scope. If wanted, add a Room read-through cache for locations/boxes/items in a follow-up; write-behind sync is a much bigger commitment and should be a separate project.
3. **Distribution** — Play Store listing vs. GitHub Releases APK. Self-hosted users are well served by the latter; Play adds review overhead and a privacy-policy requirement (the legal pages already exist).
4. **Repo layout** — sibling folder in this monorepo (`storage-labels-android/`, consistent with `-api`/`-ui` and lets CI version everything together) vs. a separate repo. Recommendation: monorepo sibling.
5. **Min SDK 26 vs 24** — 26 unless telemetry says otherwise.

## 8. Known contract issues to fix along the way

- `Api.Image.uploadImage` is typed `post<string>` in the web client but the API returns `ImageMetadata` (`UploadImage.cs:89`). Model Android off the server; consider fixing the TS type too.
- `image.ts` exposes `getImageUrl(hashedUserId, imageId)` but the live route is `images/{imageId:guid}` — dead/legacy signature, don't port it.
- `SearchResultResponse.locationId` is `string` in the TS models while locations use `number` ids elsewhere. Pick one in the Kotlin DTO (parse to `Long`) and note the inconsistency.
- The code-color-pattern parser is duplicated verbatim in `formatted-code.tsx` and `label-item.tsx`. Port it once in Kotlin.
