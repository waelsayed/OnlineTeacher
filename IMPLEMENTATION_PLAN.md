# Online Teacher — Implementation Plan

## Purpose

This document defines the current implementation roadmap for the Online Teacher project.

It is an execution plan, not a replacement for the project's architecture or business documentation.

The project documentation remains the source of truth for:

* Product requirements
* Business rules
* Domain behavior
* Architecture
* Database design
* API design

This document defines **what should be implemented next and in what order**.

---

# 1. Development Strategy

The project must be implemented incrementally using small, reviewable phases.

Do not implement the entire system at once.

The development flow is:

```text
Inspect
    ↓
Plan
    ↓
Implement
    ↓
Test
    ↓
Verify
    ↓
Report
    ↓
Human Review
    ↓
Approval
    ↓
Next Step
```

The AI Agent must stop after each major step and wait for approval when requested by `AGENTS.md`.

---

# 2. Phase 0 — Project Discovery

Before modifying the project:

1. Inspect the repository.
2. Read all available documentation.
3. Inspect the existing source code.
4. Inspect project files and dependencies.
5. Inspect database configuration.
6. Inspect Docker configuration.
7. Inspect existing tests.
8. Compare implementation against documentation.

Do not modify application code during this phase.

Produce a Project Discovery Report containing:

* Current project structure
* Existing technologies
* Existing architecture
* Existing implementation
* Missing components
* Documentation/code inconsistencies
* Risks
* Recommended implementation order
* Decisions requiring approval

After the report, stop.

---

# 3. Phase 1 — Foundation + First Vertical Slice

## Objective

Build the minimum technical foundation required to prove that the core architecture works end-to-end.

The first slice must validate:

```text
Central Platform
        ↓
Teacher Registration
        ↓
Teacher Platform Creation
        ↓
Activation
        ↓
Authentication
        ↓
Tenant Resolution
        ↓
Authorization
        ↓
Teacher Platform Access
```

The goal is not to implement the complete product.

The goal is to prove the architecture.

---

# 4. Step 0 — Scaffolding & Tooling

Create or verify:

* `.gitignore`
* `.env.example`
* `docker-compose.yml`
* Solution file
* .NET 10 projects
* Test projects
* Basic project configuration

The backend must use:

```text
ASP.NET Core Web API
.NET 10
C#
PostgreSQL
Docker
```

Expected project separation:

```text
API
Application
Domain
Infrastructure
Tests
```

Follow the architecture already documented in the repository.

Do not create unnecessary projects.

---

# 5. NuGet Dependencies

Use only packages required by the implementation.

Expected dependencies for the first phase include:

### Infrastructure

```text
Npgsql.EntityFrameworkCore.PostgreSQL
Microsoft.EntityFrameworkCore.Design
```

### API

```text
Microsoft.AspNetCore.Authentication.JwtBearer
```

### Integration Tests

```text
Testcontainers.PostgreSql
```

### Tests

```text
xunit
FluentAssertions
```

Package versions must be compatible with **.NET 10**.

Do not blindly copy package versions from old examples.

Before adding a package, verify that it is actually required.

---

# 6. Step 1 — Domain Foundation

Implement only the domain objects required for the first vertical slice.

Initial entities:

```text
Teacher
TeacherPlatform
Permission
Role
RolePermission
TeacherPlatformMembership
```

Platform status:

```text
PendingActivation
Active
Deactivated
```

The model should support:

```text
Teacher
   ↓
TeacherPlatform
   ↓
TeacherPlatformMembership
   ↓
Role
   ↓
RolePermission
   ↓
Permission
```

The platform owner must be represented through the membership model.

Do not create specialized assistant roles.

---

# 7. Public Identity

Teacher Platform must have a stable public identifier separate from its internal database identifier.

The public identifier must:

* Be non-sequential
* Be cryptographically generated
* Be stable
* Be safe for use in public URLs

The public URL format is:

```text
/{publicId}/{slug}
```

The internal database ID must never be used as the public identifier.

---

# 8. Slug

The slug must be:

* URL-safe
* Canonical
* Normalized
* Deterministic from the supplied platform name where appropriate

The route must validate both:

```text
PublicId
+
Slug
```

Rules:

### Valid PublicId + Current Slug

```text
200 OK
```

### Valid PublicId + Old/Incorrect Slug

```text
301 Permanent Redirect
```

to the current canonical URL.

### Invalid PublicId

```text
404 Not Found
```

Never resolve a Teacher Platform using PublicId alone when the supplied slug is incorrect.

---

# 9. Step 2 — Infrastructure

Implement:

```text
ApplicationDbContext
ITenantContext
TenantContext implementation
EF Core configuration
Database migrations
```

PostgreSQL is the database provider.

Tenant-owned entities must support tenant-aware data access.

Use EF Core query filters where appropriate as a **data-layer defense**.

Query filters must not replace application-level authorization.

The security model should be:

```text
Authentication
    ↓
Tenant Resolution
    ↓
Authorization
    ↓
Tenant-aware Data Access
```

---

# 10. Tenant Isolation

Tenant isolation is a critical security requirement.

The system must prevent:

```text
Teacher Platform A
        ↓
Accessing
        ↓
Teacher Platform B
```

Tenant isolation must be tested using integration tests.

Do not rely only on developer discipline to add tenant conditions manually.

The data access architecture should make accidental cross-tenant access difficult.

---

# 11. Database

PostgreSQL must run in Docker for local development.

Use:

```text
postgres:16-alpine
```

unless an explicit project decision changes the version.

The container must provide:

* Persistent named volume
* Database name through environment variables
* Username through environment variables
* Password through environment variables
* Configurable host port
* PostgreSQL health check

Credentials must never be hard-coded.

Provide:

```text
.env.example
```

with safe placeholder values.

Do not commit the actual `.env` file.

---

# 12. Password Handling

Use:

```text
Microsoft.AspNetCore.Identity.PasswordHasher<T>
```

for password hashing.

Do not implement a custom password hashing algorithm.

Do not store plaintext passwords.

Do not log passwords.

---

# 13. Step 3 — Application Layer

Implement only the application services/use cases required for the first slice.

Required operations:

```text
RegisterTeacher
CreateTeacherPlatform
ActivateTeacherPlatform
Authenticate
ResolveTenantRoute
```

Each operation must:

* Validate input
* Apply business rules
* Respect authorization
* Respect tenant boundaries
* Return an appropriate application result

Do not expose persistence entities directly to the API.

---

# 14. Authentication

Use:

```text
JWT Bearer Authentication
```

The initial JWT should contain the claims required by the documented authorization model.

At minimum, where applicable:

```text
sub = TeacherId
tenant = TeacherPlatform PublicId
permissions = permission codes
roles = role information
```

Do not put unnecessary sensitive information inside the JWT.

Authorization must be enforced server-side.

---

# 15. Permission-Based Authorization

Implement permission-based authorization.

Example:

```text
Platform.Access
```

The API must be able to enforce:

```text
[RequirePermission("Platform.Access")]
```

or the equivalent documented mechanism.

The permission handler must read trusted permission claims and enforce them server-side.

Never trust permissions supplied by the client.

---

# 16. Step 4 — API

Initial endpoints:

### Central

```http
POST /api/central/teachers/register
POST /api/central/platforms
POST /api/central/platforms/{publicId}/activate
```

### Authentication

```http
POST /api/auth/login
```

### Teacher Platform

```http
GET /{publicId}/{slug}/api/platform/me
```

The final endpoint must prove all of the following:

1. Route resolution
2. Public ID validation
3. Slug validation
4. Tenant resolution
5. Authentication
6. Permission authorization
7. Tenant isolation

---

# 17. Error Handling

Implement centralized exception handling.

Return consistent RFC-compatible Problem Details responses where appropriate.

Handle at least:

```text
Validation
Authentication
Authorization
NotFound
BusinessRuleViolation
TenantMismatch
Concurrency
Unexpected Error
```

Production responses must not expose:

* Stack traces
* Internal exception details
* Database information
* Secrets
* Sensitive implementation details

---

# 18. Logging

Use built-in ASP.NET Core structured logging initially.

Use JSON console logging where appropriate.

The logging implementation must be environment-aware.

Do not introduce Serilog unless there is a concrete requirement.

If logging requirements grow enough to justify Serilog, document the reason before introducing it.

---

# 19. Step 5 — Testing

Testing is part of implementation, not a later activity.

## Unit Tests

Cover at minimum:

* Slug normalization
* Public ID generation
* Public ID non-sequential behavior
* Platform activation rules
* Permission/claim construction

## Integration Tests

Use:

```text
Testcontainers PostgreSQL
```

Integration tests should verify the complete slice:

```text
Register
 ↓
Create Platform
 ↓
Activate
 ↓
Login
 ↓
Resolve Tenant
 ↓
Access Platform
```

Also verify:

* Teacher A cannot access Teacher B data
* Invalid PublicId → 404
* Wrong slug → 301 canonical redirect
* Unauthorized request → 401
* Authenticated user without permission → 403
* Duplicate email
* Duplicate platform identifiers where applicable
* Invalid state transitions

---

# 20. Transactional Operations

Operations that change multiple pieces of related data must be atomic.

For example:

```text
Create Teacher
+
Create Platform
+
Create Owner Membership
+
Create Owner Role
+
Assign Owner Permissions
```

must not leave partially created data if the operation fails.

Use appropriate database transactions.

Do not create distributed transactions.

---

# 21. Concurrency

Protect operations that can be executed concurrently.

Examples:

* Platform activation
* User registration
* Duplicate resource creation
* Permission changes

Use database constraints and appropriate concurrency mechanisms.

Do not depend only on application-level checks such as:

```text
if (!exists)
    insert
```

without considering concurrent requests.

---

# 22. Verification

After implementation:

```bash
docker compose up -d
```

Verify PostgreSQL becomes healthy.

Then:

```bash
dotnet build
```

Target:

```text
0 warnings
0 errors
```

Then:

```bash
dotnet test
```

All tests must pass.

Finally perform a manual smoke test of the first vertical slice.

---

# 23. Git Strategy

Keep commits small and logically isolated.

Preferred sequence:

```text
scaffold
domain
infrastructure
application
api
docker
tests
verification
```

Do not mix unrelated refactoring with feature implementation.

Each commit should leave the repository in a comprehensible state.

---

# 24. Implementation Status

The Agent must maintain this section as implementation progresses.

Example:

```text
Phase 0 — Project Discovery       [x] Completed
Step 0 — Scaffolding              [x] Completed
Step 1 — Domain                   [x] Completed
Step 2 — Infrastructure           [x] Completed
Step 3 — Application              [x] Completed
Step 4 — API                      [x] Completed
Step 5 — Docker                   [x] Completed
Step 6 — Tests                    [x] Completed
Step 7 — Verification             [ ] Pending
Step 8 — Git                      [ ] Pending
```

Use:

```text
[ ] Pending
[-] In Progress
[x] Completed
[!] Blocked
```

Do not mark a step completed unless it has been verified.

---

# 25. Decision Log

Record implementation decisions here.

Current approved decisions:

```text
- Single solution
- Central Platform + Teacher Platform areas
- ASP.NET Core Web API
- .NET 10
- PostgreSQL
- Docker for PostgreSQL
- JWT Bearer authentication
- Role + Permission authorization
- Dynamic permissions
- EF Core tenant query filter as a data-layer guard
- Testcontainers for PostgreSQL integration tests
- Built-in structured JSON logging initially
- PublicId + Slug routing
- Wrong valid PublicId/Slug combination redirects to canonical URL with 301

Step 0 additions (scaffold):

```text
- Solution format: .slnx (SDK 10 default)
- Package versions resolved for .NET 10: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3,
  Microsoft.EntityFrameworkCore.Design 10.0.11, Microsoft.AspNetCore.Authentication.JwtBearer 10.0.11,
  Testcontainers.PostgreSql 4.14.0, FluentAssertions 8.10.0
- docker-compose defaults carry safe placeholder credentials so the stack runs without a .env file;
  a real .env must never be committed
- PostgreSQL 16 Alpine in Docker with persistent named volume (pgdata) and pg_isready healthcheck

Step 1 additions (domain foundation):

```text
- PublicId value object: 12-char base62, cryptographically generated (RandomNumberGenerator);
  global uniqueness enforced by DB unique constraint in a later step
- Slug value object: canonical pattern ^[a-z0-9]+(-[a-z0-9]+)*$, max 60 chars;
  normalization from platform name with deterministic fallback "platform" when no URL-safe
  characters are present (e.g. Arabic-only names); slugs are NOT globally unique
- Email value object with lightweight format validation
- PlatformStatus states: PendingActivation -> Active -> Deactivated;
  Activate only from PendingActivation, Deactivate only from Active
- Platform ownership represented through TeacherPlatformMembership (Owner role + IsOwner flag)
- Domain layer has zero external dependencies (no ASP.NET Core/EF/HTTP packages)
  and is independent of ASP.NET Core, EF Core, PostgreSQL, and JWT
```

Step 2 additions (infrastructure):

```text
- EF Core version unified to 10.0.11 across the graph: Microsoft.EntityFrameworkCore and
  Microsoft.EntityFrameworkCore.Relational are explicit public references in Infrastructure so Api and
  IntegrationTests bind to the same runtime used by Microsoft.EntityFrameworkCore.Design 10.0.11
  (prevents MSB3277 assembly conflicts when consumers combine Npgsql's 10.0.4 transitive EF with 10.0.11)
- Scoped TenantContext holds at most one tenant per DI scope and rejects mid-scope tenant switches;
  ApplicationDbContext exposes TenantId through the ITenantContext port
- EF Core global query filters applied ONLY to tenant-scoped entities (Role, RolePermission,
  TeacherPlatformMembership); global entities (Teacher, TeacherPlatform, Permission) are NOT filtered
- Password hashing infrastructure via PasswordHasher<Teacher> behind an IPasswordHasher port;
  no authentication/login/JWT implemented in Step 2
- Value converters for Email/PublicId/Slug expose static fields named EmailConverter/PublicIdConverter/
  SlugConverter because bare names shadowed the type aliases during compilation
- Design-time factory (IDesignTimeDbContextFactory) reads ConnectionStrings__DefaultConnection and
  otherwise builds a connection string from POSTGRES_* env vars with docker-compose placeholder defaults
- InitialCreate migration generated (dotnet ef tools 10.0.7) and applied to the Docker PostgreSQL dev
  database; constraints verified live (duplicate slug allowed, duplicate PublicId/Email rejected)
```

Step 3 additions (application layer):

```text
- One service per approved use case: RegisterTeacherService, CreateTeacherPlatformService,
  ActivateTeacherPlatformService, AuthenticateTeacherService, TenantRouteResolver
- Purpose-specific persistence ports only: ITeacherRepository, IPlatformRepository (by PublicId),
  IRoleRepository, IPermissionRepository (by code) + IUnitOfWork (single SaveChangesAsync commit)
- Atomicity: each use case stages its full graph through repositories and commits with one
  SaveChangesAsync (EF wraps a single save in a transaction); no Unit of Work abstraction beyond
  the minimal commit port (avoids a generic UoW)
- Duplicate email is enforced by the DB unique constraint only and translated by the persistence
  layer into DuplicateEmailException (no application-side existence pre-check, so no race window)
- CreateTeacherPlatform builds the full tenant graph atomically: platform (PendingActivation,
  cryptographic PublicId, deterministic slug from name), Owner role, owner permissions from the
  global catalog, and the owning teacher's membership (IsOwner); duplicate and non-unique slugs
  allowed with no uniqueness check
- Owner role creation depends on the global permission catalog being present; a missing permission
  aborts creation before anything is saved (data seeding deferred to the composition/infrastructure
  step without running a new migration here)
- Central use cases (Register/Create/Activate/Authenticate) refuse to run under an active tenant
  scope (TenantMismatchException); Create sets its own new-tenant scope for the tenant-scoped writes
  and clears it afterwards
- Authentication returns a generic failure for both unknown email and wrong password so stored
  hashes and email existence are never revealed; JWT stays out of the Application layer
- ResolveTenantRoute is keyed only by PublicId; invalid/unknown PublicId -> NotFound, wrong slug ->
  Redirect carrying the canonical PublicId+slug; slug is never queried alone
- Concurrency: domain state guard (PendingActivation/testable state transitions) + single atomic
  SaveChanges + DB unique constraints; a dedicated optimistic concurrency token is intentionally
  deferred (documented risk) to keep this step free of model/migration changes
- Application project has zero package references (no EF/Npgsql/ASP.NET/JWT); dependency direction
  is strictly Application -> Domain
```

Step 4 additions (API & composition layer):

```text
- Composition root (Program.cs): reads ConnectionStrings:DefaultConnection and falls back to
  ConnectionFactory.Build(); registers DbContext, TenantContext, repositories, IUnitOfWork, all
  Application use cases, JwtTokenFactory, permission policy provider/handler, and the tenant-route
  middleware; startup runs MigrateAsync + PermissionSeeder (scoped) deterministically
- JwtOptions bound via the Options pipeline; JWT issuer/audience/signing key come from configuration/
  environment only (no hard-coded secrets), with dev-only placeholder key in appsettings.Development.json
- JWT claims: sub = TeacherId, tenant = Teacher Platform PublicId, roles (ClaimTypes.Role),
  permission codes (Permission:) and isOwner; never passwords/hashes/sensitive data
- IdentityModel 8+/JWT bearer requires a matching "kid" header for signature validation, so the
  symmetric signing key gets a stable configured KeyId (JwtOptions.KeyId, default "OnlineTeacher.SigningKey")
  used by both issuance and validation
- JWT validation configuration is resolved from the SAME JwtOptions (via IOptions) that issues tokens,
  so a host (e.g. WebApplicationFactory) overriding Jwt config is honored consistently; avoids
  divergence between a separate Get<JwtOptions>() snapshot and the IOptions instance
- AddJwtBearer validation via JwtBearerOptions PostConfigure; TokenValidationParameters validate
  issuer, audience, signing key (with KeyId) and lifetime (30s clock skew)
- Login (POST /api/auth/login) requires a publicId: the JWT is platform-scoped, so the caller selects
  the tenant; GetTeacherPlatformAccessService resolves that platform and builds the access profile.
  (Owner decision: require publicId rather than auto-resolve a teacher's single platform)
- RequirePermission("...") derives from AuthorizeAttribute and maps to a dynamically resolved policy
  (PermissionPolicyProvider prefix "Permission:"); PermissionHandler only trusts server-issued
  permission claims; authentication and authorization remain separate concerns
- TenantRouteMiddleware processes routes carrying {publicId} + {slug}: NotFound -> 404, wrong slug ->
  301 to the canonical URL, matching slug -> establish tenant context. The 301 Location preserves the
  path that follows the slug segment (e.g. /api/platform/me) so following the redirect reaches the same
  endpoint instead of a non-existent route
- ExceptionHandlingMiddleware maps application exceptions to consistent RFC 7807 ProblemDetails
  (Validation 400, NotFound 404, DuplicateEmail 409, BusinessRule 422, TenantMismatch 403,
  Concurrency 409, Authentication 401, generic 500 with dev-only detail)
- Platform creation threads the membership explicitly (ITeacherRepository.AddMembership -> Memberships.Add)
  in addition to adding to the teacher aggregate, because the membership has TWO relationships to
  TeacherPlatform (TeacherPlatformId AND TenantId) and EF relationship fixup mis-classified the new
  membership as Modified (UPDATE missing row -> DbUpdateConcurrencyException); explicit add makes the
  insert deterministic
- .env.example documents ConnectionStrings__DefaultConnection and Jwt__* overrides; a real .env is
  never committed
- Integration tests use Testcontainers PostgreSQL 16 + WebApplicationFactory<Program> (Microsoft.AspNetCore
  .Mvc.Testing 10.0.11), shared collection fixture, per-test unique emails, and a client with
  AllowAutoRedirect=false so redirect/301 behavior is asserted directly
- Security behavior (SECURITYFIX then FINALSTEP4 refinement): an initial middleware-level JWT-tenant
  binding was added (SECURITYFIX) that rejected any authenticated request whose "tenant" claim differed
  from the route publicId, but this was reversed/refined (FINALSTEP4) because the product MUST allow an
  authenticated user to browse another tenant's PUBLIC content. TenantRouteMiddleware now RESOLVES the
  tenant from {publicId}/{slug} and establishes the TenantContext but does NOT reject an authenticated
  request solely because its JWT "tenant" claim differs from the route tenant. Protected tenant-management
  endpoints enforce tenant access through the existing authorization and application security (permission
  policies + membership checks in application services), keeping defense-in-depth. NotFound (404),
  canonical-redirect (301) and central (non-tenant) endpoints behavior is unchanged. Unit tests
(TenantRouteMiddlewareTests) prove the middleware neither rejects an authenticated cross-tenant request
   nor an anonymous request.
```

Step 5 additions (Dockerization & deployment hardening):

```text
- Multi-stage Dockerfile (mcr.microsoft.com/dotnet/sdk:10.0 build -> aspnet:10.0 final) restoring and
  publishing only the API project graph (not the whole solution, which also references the test projects);
  framework-dependent publish (UseAppHost=false), EXPOSE 8080, ENTRYPOINT dotnet OnlineTeacher.Api.dll
- docker-compose now runs both services: postgres (unchanged, service_healthy) and api (built from the
  Dockerfile, depends_on postgres condition: service_healthy so it starts only after PostgreSQL is healthy)
- API runtime configuration is environment-driven: ASPNETCORE_ENVIRONMENT=Production, ASPNETCORE_URLS=http://+:8080,
  POSTGRES_HOST/PORT/DB/USER/PASSWORD (consumed by ConnectionFactory), and Jwt__Issuer/Audience/SigningKey/
  TokenLifetimeMinutes from compose placeholders; no secrets committed and a real .env is never committed
- Added a lightweight liveness /health endpoint (Microsoft.Extensions.Diagnostics.HealthChecks,
  app.MapHealthChecks("/health")) for container orchestration; it is unauthenticated and sits outside the
  tenant route
- EF Core migrations + permission seeding run at container startup via the existing Program.cs startup
  path; verified idempotent against an already-migrated database ("No migrations were applied. The database
  is already up to date.")
- Structured JSON console logging retained (built-in AddJsonConsole), suitable for Docker log collection
- .env.example updated with POSTGRES_HOST and API_PORT/Jwt placeholders; docker-compose.yml documents the
  stack inline
- Verified end-to-end from a clean-build container: register -> create platform -> login -> activate ->
  /api/platform/me; wrong-tenant 403; wrong-slug 301 preserves the endpoint suffix; invalid PublicId 404
```

Step 6 additions (testing):

```text
- Filled the section 19 gap on "Permission/claim construction" and authorization unit tests that were
  previously only exercised end-to-end: added JwtTokenFactoryTests (sub, tenant, isOwner, all role claims,
  all permission claims, issuer/audience, configured lifetime, kid header, and never emits password/hash
  material) and PermissionAuthorizationTests (PermissionHandler grants only on the exact server-issued
  permission claim and denies when absent/different; PermissionPolicyProvider maps "Permission:<code>" to a
  PermissionRequirement and forwards unknown policies to the default provider; RequirePermissionAttribute
  builds the dynamic "Permission:<code>" policy name). All live under tests/OnlineTeacher.UnitTests/Api,
  consistent with the existing TenantRouteMiddlewareTests
- Added a missing section 19 integration scenario: activating an already-active platform returns 422
  (BusinessRuleViolation), proving invalid state transitions are rejected at the API boundary
- Remaining section 19 integration scenarios (slice, tenant isolation, invalid PublicId 404, wrong slug 301,
  canonical slug, 401, 403, duplicate email 409, duplicate slug allowed) were already covered by the existing
  ApiScenarioTests; duplicate platform `publicId` collision is not integration-testable because publicIds are
  cryptographically unique per platform
- Verification: dotnet build --warnaserror => 0 warnings / 0 errors; unit tests 135/135 passed;
  integration tests 14/14 passed against a real PostgreSQL 16 Testcontainer via WebApplicationFactory
```

If a decision must change:

1. Explain why.
2. Describe the impact.
3. Propose the smallest alternative.
4. Request human approval.
5. Update this decision log only after approval.

---

# 26. Scope Control

Do not implement features from future phases while working on Phase 1.

Do not add:

* Courses
* Lessons
* Exams
* Homework
* Wallet
* Coupons
* Posts
* Messaging
* Notifications

during Phase 1 unless specifically required to validate the foundation.

The purpose of Phase 1 is architectural validation.

---

# 27. Definition of Done

Phase 1 is complete only when:

* The solution builds successfully.
* PostgreSQL runs successfully in Docker.
* Database migrations work.
* Teacher registration works.
* Teacher Platform creation works.
* Platform activation works.
* Authentication works.
* JWT authorization works.
* Permission authorization works.
* Tenant resolution works.
* PublicId + Slug validation works.
* Canonical redirect works.
* Tenant isolation is tested.
* API error handling works.
* Unit tests pass.
* Integration tests pass.
* No known critical security issue exists.
* The implementation matches the approved architecture.
* Changes are documented.
* Git history is clean and reviewable.

---

# 28. Agent Rule

Never interpret this document as permission to skip human review.

The Agent is responsible for implementation.

The human project owner has final authority over:

* Business rules
* Architecture
* Technology decisions
* Security decisions
* Database strategy
* Scope

When uncertain, stop and ask.

**Do not guess.**
