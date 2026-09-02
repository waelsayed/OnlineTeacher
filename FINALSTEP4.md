## Step 4 — Final Refinement: Do Not Block Authenticated Public Tenant Browsing

We confirmed an architectural issue in the current `EnforceTenantBinding` implementation.

The product MUST allow an authenticated user/teacher to browse another teacher's **public platform content**.

Example:

```text
Teacher A is authenticated
JWT tenant = Teacher A

Teacher A opens:

/{TeacherB-PublicId}/{TeacherB-Slug}/public-content

Expected:
200 / allowed
```

But Teacher A must NOT be allowed to access Teacher B's protected platform-management resources:

```text
Teacher A JWT
→ /{TeacherB-PublicId}/{TeacherB-Slug}/api/platform/me
→ 403
```

### Required architectural behavior

Do NOT make `TenantRouteMiddleware` globally reject every authenticated request whose JWT tenant differs from the route tenant.

The tenant middleware's primary responsibility is:

1. Resolve the tenant from `{publicId}/{slug}`.
2. Establish the `TenantContext`.
3. Preserve:

   * invalid PublicId → 404
   * canonical slug → continue
   * wrong slug → 301 canonical redirect
4. Do NOT block authenticated cross-tenant requests solely because their JWT tenant differs.

Protected endpoints must enforce tenant access through the existing authentication/authorization/application security mechanisms.

### Important

Do NOT implement a large/general "public endpoint classification" system now.

The Wall/public-content endpoints do not exist yet, so do not invent them.

Simply remove/refine the overly broad middleware-level binding so that future `[AllowAnonymous]` / public tenant endpoints can work for authenticated users as well.

Keep the existing protection for the current protected endpoint:

```text
/{publicId}/{slug}/api/platform/me
```

and ensure:

```text
Teacher A JWT → Teacher B protected endpoint → 403
```

still passes.

### Tests

Update/add integration coverage for the currently existing behavior.

At minimum verify:

1. Teacher A JWT → Teacher A protected endpoint → allowed.
2. Teacher A JWT → Teacher B protected endpoint → 403.
3. Anonymous → tenant route → existing authentication/authorization behavior remains unchanged.
4. Invalid PublicId → 404.
5. Wrong slug → 301 with the full endpoint suffix preserved.
6. Token without tenant claim → preserve the correct behavior for protected endpoints.

Since no public tenant endpoint currently exists, do NOT create a fake Wall endpoint just for testing.

If useful, add a small middleware-level/unit test proving that tenant middleware itself does not reject an authenticated request solely because the JWT tenant differs from the route tenant. Do not create unnecessary infrastructure.

### Scope restrictions

* Do NOT start Step 5.
* Do NOT implement Wall/posts.
* Do NOT implement public endpoint classification.
* Do NOT redesign authorization.
* Do NOT introduce new abstractions unless strictly necessary.
* Do NOT modify unrelated functionality.
* Keep the existing defense-in-depth checks in the application layer.

### Verification

Run:

```bash
dotnet build --warnaserror
dotnet test
```

Run integration tests with PostgreSQL/Testcontainers.

Expected:

* Build: 0 warnings / 0 errors
* Unit tests: all pass
* Integration tests: all pass

Create a focused follow-up commit:

```text
security: allow authenticated public tenant browsing
```

Update `IMPLEMENTATION_PLAN.md` / Decision Log only as necessary.

After verification, STOP and report:

* exact files changed
* exact security behavior after the change
* tests added/changed
* build/test results
* commit hash
* remaining concerns

Do not proceed to Step 5. Wait for human approval.
