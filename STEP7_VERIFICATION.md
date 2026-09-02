# Step 7 — Verification: Full Containerized Smoke Test

Date: 2026-09-02 · Baseline commit: `d338e6a` (Step 6) · Environment: Windows, Docker 26.1.1, .NET 10 SDK

## Goal

Prove the Online Teacher backend works end-to-end from a clean Docker Compose environment (not
just through unit/integration tests).

## Environment & Commands

```bash
# Tear down any prior project, then start clean (API image build is cached)
docker compose down
docker compose up -d --build
docker compose ps
```

### Container state

| Service | Status |
|---------|--------|
| `onlineteacher-postgres` | Up (healthy) — postgres:16-alpine |
| `onlineteacher-api` | Up — image `onlineteacher-api` |

### Proven running from Docker (not the dev host)

- `docker ps` shows `onlineteacher-api` (image `onlineteacher-api`).
- Port `8080` is owned by Docker networking (`com.docker.backend` / `wslrelay`), not by a native process.
- No native `OnlineTeacher.Api` process exists.

## Health & Startup

- `/health` → **200** `Healthy`.
- Startup logs: EF migrations ran idempotently (`No migrations were applied. The database is already
  up to date.`) and seeded `Platform.Access` / `Platform.Manage`.
- Hosting environment: `Production`; listening on `http://[::]:8080`.

## End-to-End Flow (Dockerized API)

All steps executed over HTTP against `http://localhost:8080`.

| # | Step | Result |
|---|------|--------|
| 1 | Register Teacher `POST /api/central/teachers/register` | Created (TeacherId issued) |
| 2 | Create Platform `POST /api/central/platforms` | Created, status `PendingActivation`, publicId + slug issued |
| 3 | Login `POST /api/auth/login` | OK, JWT issued |
| 4 | Activate `POST /api/central/platforms/{publicId}/activate` | OK, `activatedAtUtc` set |
| 5 | `GET /{publicId}/{slug}/api/platform/me` | 200 — status=Active, isOwner=true, roles=Owner, permissions=Platform.Access, Platform.Manage |

## Authorization & Routing Behavior

| Scenario | Expected | Observed | Library Status |
|----------|----------|----------|----------------|
| Valid tenant + correct slug → protected access | 200 | **200** | Pass |
| Authenticated Teacher A → Teacher B protected `/api/platform/me` | 403 | **403** | Pass |
| Correct PublicId + wrong slug | 301 → canonical | **301** `Location: /{publicId}/{slug}/api/platform/me` (API suffix preserved) | Pass |
| Invalid/nonexistent PublicId | 404 | **404** | Pass |
| Anonymous access to protected endpoint | 401 | **401** | Pass |
| `/health` | available + healthy | **200** | Pass |

### Tenant isolation confirmed at the data layer

Container logs show EF Core global query filters applying `tenant_id` on memberships, roles, and
role_permissions, so cross-tenant access is also blocked at the data layer (defense in depth) in
addition to the 403 from application authorization.

## Log Health

- No `LogLevel: Error` / `LogLevel: Critical` entries, no exceptions, and no `5xx` responses during
  the smoke test.
- Only benign startup warnings (non-blocking, do not affect functionality):
  - ASP.NET Data Protection key persistence location in the ephemeral container.
  - Npgsql Kerberos/GSSAPI library (`libgssapi_krb5.so.2`) not present — password authentication is
    unaffected; JWT uses its own symmetric signing.
  - HTTP_PORTS override to `http://+:8080` (by design).

## Issues Encountered

None that required a code change. The `Invoke-WebRequest -MaximumRedirection 0` calls raise a
"maximum redirection count exceeded" exception by design when confirming the 301; the `301` and its
`Location` header are still retrieved and printed.

## Cleanup & Final Repository Status

- Test environment torn down: `docker compose down` (the `pgdata` named volume is retained for dev reuse).
- Repository left clean except intended Step 7 documentation/status changes:
  - `IMPLEMENTATION_PLAN.md`: Step 7 marked `[x] Completed` + Step 7 entry in the decision log.
  - `STEP7.md` remains an untracked instruction file (not part of the committed change set).

## Result

All required verification scenarios passed against the clean containerized environment. No
application defect was discovered, so no production code or tests were modified during this step.