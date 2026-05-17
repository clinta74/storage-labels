# Integration Test Coverage

83 tests across 8 feature areas.

---

## Users (11 tests)

- `GET /user` — unauthenticated → 401; seeded user → 200 with data
- `POST /user` — creates user → 200; duplicate → 409
- `GET /user/exists` — seeded → true; unknown → false; unauthenticated → 401
- `GET /user/preferences` — seeded → 200 with defaults; unknown → 404
- `PUT /user/preferences` — valid data → 200; persists across requests; unknown user → 404

---

## Locations (11 tests)

- `GET /locations` — unauthenticated → 401; returns only owned locations
- `POST /locations` — valid → 201; appears in list
- `GET /locations/{id}` — by ID → 200; non-existent → 404; other user's → 404
- `PUT /locations/{id}` — valid → 200; non-existent → 404
- `DELETE /locations/{id}` — empty → 200; with boxes without force → 422; with boxes + force → 200; non-existent → 404

---

## Boxes (13 tests)

- `GET /boxes` — unauthenticated → 401; by location → empty list
- `POST /boxes` — valid → 201; duplicate code in same location → 409
- `GET /boxes/{id}` — after create → 200; non-existent → 404
- `PUT /boxes/{id}` — valid → 200; non-existent → 404
- `PATCH /boxes/{id}/location` — move to another location → 200
- `DELETE /boxes/{id}` — empty → 200; with items without force → 422; with items + force → 200
- DB reset sanity: Respawn clears data between tests

---

## Items (9 tests)

- `GET /items` — unauthenticated → 401; by box → empty list; after create → returns item
- `POST /items` — valid → 201; empty name → 422
- `GET /items/{id}` — after create → 200; non-existent → 404
- `PUT /items/{id}` — valid → 200 with updated fields; non-existent → 404
- `DELETE /items/{id}` — → 200, no longer found; non-existent → 404

---

## Images (12 tests)

- `GET /images` — unauthenticated → 401; empty → 200 empty list; after upload → returns metadata
- `POST /images` — unauthenticated → 401; valid JPEG → 200; non-JPEG → 400
- `GET /images/{id}/file` — unknown ID → 404; after upload → 200
- `DELETE /images/{id}` — unknown ID → 404; after upload → 200; after already deleted → 404
- `DELETE /images/{id}?force=true` — after upload → 200

---

## Search (8 tests)

- `GET /search/qr/{code}` — unauthenticated → 401; matching code → 200; non-existent → 404; other user's box → 404
- `GET /search?q=` — unauthenticated → 401; valid query → 200 with results; empty DB → empty list; `X-Total-Count` header present

---

## Common Locations (10 tests)

- `GET /common-locations` — unauthenticated → 401; authenticated → empty list; after create → appears in list
- `POST /common-locations` — with permission → 201; without permission → 403; empty name → 422
- `DELETE /common-locations/{id}` — after create → 200; after delete → no longer in list; non-existent → 404; without permission → 403

---

## Encryption Keys (17 tests)

- `GET /encryption-keys` — unauthenticated → 401; with read permission → empty list; after create → list
- `POST /encryption-keys` — unauthenticated → 401; valid → 200; no description → 200
- `GET /encryption-keys/{kid}/stats` — unknown KID → 404; after create → 200
- `POST /encryption-keys/{kid}/activate` — unknown → 404; after create → 200
- `POST /encryption-keys/{kid}/retire` — unknown → 404; after activate → 200
- `GET /encryption-keys/rotations` — with read permission → list
- `GET /encryption-keys/rotations/{id}` — unknown ID → 404
- `POST /encryption-keys/rotations` — valid request → 202 with rotation ID
- `DELETE /encryption-keys/rotations/{id}` — unknown ID → 404
