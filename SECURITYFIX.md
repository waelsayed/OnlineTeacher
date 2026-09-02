### Step 4 — Final Security Fix: Bind JWT Tenant to Tenant Route

Step 4 is functionally complete, but before approving Step 4 and moving to Step 5, fix one architectural/security gap found during code review.

#### Problem

The current `TenantRouteMiddleware` resolves the tenant from:

`/{publicId}/{slug}/...`

and sets `TenantContext`, but it does not directly enforce that the authenticated JWT `tenant` claim matches the `publicId` from the route.

Currently some application services perform membership/tenant checks, which protects the existing endpoints, but tenant isolation must not depend on every future service/controller remembering to perform this check.

We need this invariant at the middleware/security boundary:

```text
JWT tenant claim
        ↓
must match
        ↓
Route PublicId
        ↓
TenantContext
```

#### Required changes

1. Review the existing JWT claims implementation and `TenantRouteMiddleware`.

2. Make the tenant binding explicit and centralized:

   * For authenticated tenant/platform requests, read the JWT `tenant` claim.
   * Resolve the route `PublicId`.
   * The JWT tenant PublicId MUST equal the route PublicId.
   * If they do not match, reject the request with the appropriate HTTP status (403 is preferred for authenticated cross-tenant access).
   * Do not silently replace the JWT tenant with the route tenant.

3. Preserve the existing behavior:

   * Invalid/unknown PublicId → 404.
   * Correct PublicId + correct slug → continue normally.
   * Correct PublicId + wrong slug → existing 301 canonical redirect, preserving the complete endpoint suffix.
   * Central endpoints that intentionally operate without a tenant must continue to work.
   * Login must remain platform-scoped and require `PublicId`.

4. Do NOT redesign the tenant architecture.

5. Do NOT introduce new abstractions unless genuinely required.

6. Do NOT start Step 5.

7. Do NOT modify unrelated features.

#### Required tests

Add/update integration tests proving at minimum:

```text
Token A + Route Tenant A → allowed

Token A + Route Tenant B → 403

Token A + invalid/unknown Route PublicId → 404

Authenticated tenant request with missing/invalid tenant claim
→ rejected appropriately (401/403 according to the existing authentication design)
```

Also verify that the existing canonical slug redirect still works:

```text
Token A + /{TenantA}/{wrong-slug}/api/platform/me
→ 301
→ /{TenantA}/{canonical-slug}/api/platform/me
```

#### Verification

Run:

```bash
dotnet build --warnaserror
dotnet test
```

Also run the integration tests against PostgreSQL/Testcontainers as already configured.

Expected result:

* Build: 0 warnings / 0 errors
* All unit tests pass
* All integration tests pass
* No existing Step 4 behavior regresses

Update `IMPLEMENTATION_PLAN.md` / Decision Log only if necessary to document this security invariant.

Create a focused commit:

```text
security: bind JWT tenant to tenant route
```

After verification, STOP.

Do not proceed to Step 5.

Report:

* files changed
* exact tenant-binding behavior
* tests added/changed
* build/test results
* commit hash
* any remaining concerns

Wait for human review/approval before doing anything else.
