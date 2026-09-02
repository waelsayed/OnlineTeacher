Approved — proceed with **Step 4: API & Composition Layer only**.

Use the completed Step 1, Step 2, and Step 3 implementations as the source of truth.

The goal of Step 4 is to compose the existing layers and expose the approved API behavior. Do not redesign the Domain or Application layers unless a concrete integration issue makes it necessary.

### 1. Dependency Injection / Composition Root

Configure the API composition root to register:

* `ApplicationDbContext`
* `ITenantContext` / `TenantContext`
* Infrastructure repository implementations
* `IUnitOfWork`
* `IPasswordHasher<Teacher>`
* All approved Application services/use cases
* Required authentication/authorization services

Keep dependency direction intact.

Do not introduce a generic service locator or unnecessary abstraction.

### 2. JWT Authentication

Implement JWT Bearer authentication according to the approved architecture.

JWT claims must include the approved identity information, including:

* `sub` = TeacherId
* `tenant` = Teacher Platform PublicId
* roles where required
* permission codes where required

Do NOT put passwords, password hashes, or other sensitive data into JWT claims.

Use configuration/environment variables for signing configuration and secrets.

Do not hard-code JWT secrets.

### 3. Authentication Endpoint

Implement the approved login endpoint:

`POST /api/auth/login`

It should call the existing `AuthenticateTeacherService`.

Requirements:

* Validate request input.
* Return the approved authentication result.
* Generate the JWT at the API/composition boundary.
* Do not expose password/hash information.
* Preserve the generic authentication failure behavior already implemented to avoid email/account enumeration.

### 4. Central Platform Endpoints

Implement:

`POST /api/central/teachers/register`

`POST /api/central/platforms`

`POST /api/central/platforms/{publicId}/activate`

Use the existing Application services.

Do not duplicate business rules inside controllers.

Central operations must not accidentally execute under an active tenant context.

### 5. Tenant Route & Middleware

Implement tenant resolution for:

`/{publicId}/{slug}/api/platform/me`

The routing behavior is fixed and MUST remain:

1. PublicId not found → `404`
2. PublicId found + supplied slug equals canonical slug → continue
3. PublicId found + supplied slug differs from canonical slug → `301` redirect to the canonical URL
4. Slug is not globally unique.
5. Never resolve a platform by Slug alone.

Important clarification:

The Application route resolver may locate the platform by `PublicId`, but the supplied `Slug` MUST still be compared against the platform's canonical slug before allowing the request to continue.

Do not interpret "keyed only by PublicId" as "ignore the supplied slug".

The actual `301` HTTP response belongs in the API layer.

### 6. Tenant Context Security

Establish the tenant context before tenant-aware application/database access.

Maintain the approved security chain:

Authentication
→ Tenant Resolution
→ Authorization
→ Tenant-aware Data Access

Do not rely on EF query filters as the authorization mechanism.

Ensure authenticated users cannot simply supply another platform's PublicId and gain access to that tenant.

### 7. Authorization / Permissions

Implement the approved permission-based authorization model.

Use permission codes rather than creating hard-coded specialized roles.

Implement the approved:

`[RequirePermission("Platform.Access")]`

behavior.

Authorization must verify that the authenticated teacher/membership is allowed to access the resolved tenant.

Do not introduce a new role hierarchy.

### 8. Permission Catalog / Seeding

Complete the deferred permission catalog initialization required by the existing Step 3 implementation.

The existing platform permissions:

* `Platform.Access`
* `Platform.Manage`

must exist before platform creation attempts to assign them.

Keep seeding deterministic and idempotent.

Do not create additional permissions that are not currently required.

### 9. ProblemDetails / Error Handling

Implement centralized ASP.NET Core error handling using `ProblemDetails`.

Map the approved application exceptions appropriately:

* NotFoundException → 404
* ValidationException → 400
* BusinessRuleViolationException → appropriate 4xx
* TenantMismatchException → appropriate authorization/security response
* ConcurrencyException → 409
* DuplicateEmailException → 409

Do not expose:

* database exception details
* stack traces
* password/hash information
* internal implementation details

Responses should be consistent JSON `ProblemDetails`.

### 10. Logging

Implement structured JSON console logging as specified in `AGENTS.md`.

Do not log:

* passwords
* password hashes
* JWT secrets
* sensitive authentication data

Do not add Serilog unless there is a concrete requirement that cannot be satisfied by the built-in logging infrastructure.

### 11. Database / Infrastructure Composition

Implement the Infrastructure repository and UnitOfWork adapters required by Step 3.

They must use the existing `ApplicationDbContext`.

The duplicate-email translation must remain at the infrastructure persistence boundary as designed.

Do not leak EF Core exceptions into the API.

### 12. Transactions

Ensure the platform creation workflow remains atomic:

Platform

* Owner Role
* Required Permissions
* Owner Membership

must succeed or fail as one operation.

Do not introduce distributed transactions.

### 13. Concurrency

Preserve the existing Step 3 decision.

Do NOT add a new optimistic concurrency token or migration in this step merely to address the documented risk.

Keep the existing domain state guard and atomic SaveChanges behavior.

Document the concurrency limitation if needed.

### 14. API Contract

Use request/response DTOs.

Do not expose persistence entities directly.

Keep HTTP concerns in the API layer and business rules in Domain/Application.

### 15. Tests

Add integration tests for the actual API/database composition.

Use PostgreSQL through Testcontainers where appropriate.

At minimum verify:

#### Authentication

* Valid login → JWT returned
* Invalid credentials → generic authentication failure
* No sensitive data in response/token

#### Registration

* Valid registration
* Duplicate email → 409
* Invalid input → appropriate 4xx

#### Platform

* Create platform
* Initial status = PendingActivation
* Owner membership exists
* Required permissions exist
* Duplicate slug is allowed
* Duplicate PublicId is rejected

#### Activation

* PendingActivation → Active
* Invalid activation state → appropriate error
* ActivatedAtUtc populated

#### Routing

* Invalid PublicId → 404
* Valid PublicId + canonical Slug → success
* Valid PublicId + wrong Slug → 301 canonical redirect
* Duplicate Slugs do not affect PublicId-based resolution
* Slug alone can never resolve a platform

#### Tenant Isolation

Create at least two platforms/tenants and verify:

* Tenant A cannot access Tenant B's tenant-scoped data.
* Valid authentication for A does not grant access to B.
* EF tenant filters remain active.
* Authorization and tenant resolution are both enforced.

#### Authorization

* Unauthenticated request → 401
* Authenticated without required permission → 403
* Authorized request → success

### 16. Configuration

Use configuration/environment variables for:

* PostgreSQL connection string
* JWT configuration
* other secrets

Update `.env.example` if additional required environment variables are introduced.

Never commit real secrets.

### Scope Control

Do NOT implement features outside the approved Phase 1 scope.

Do not implement:

* Courses
* Students
* Wallet
* Coupons
* Exams
* Homework
* Notifications
* Messaging
* Wall
* Files/media business features
* Teacher dashboard features beyond the approved platform access endpoint

### Verification

Run:

1. `dotnet restore`
2. `dotnet build -warnaserror`
3. All unit tests
4. All integration tests
5. PostgreSQL/Testcontainers integration verification
6. Docker Compose verification
7. Manual API smoke tests where appropriate

Target:

* 0 warnings
* 0 errors
* All tests passing.

Inspect the final dependency graph and ensure no architectural boundary violations were introduced.

Update:

* `Implementation Status`
* `Decision Log`

Create one logical Step 4 commit.

Report:

* Files changed
* API endpoints
* DI/composition changes
* JWT/authorization design
* Tenant middleware behavior
* ProblemDetails mapping
* Infrastructure implementations
* Permission seeding
* Integration test results
* Build/test results
* Any warnings, assumptions, or deviations

Then **STOP**.

Do not proceed to Step 5 or any later phase until I explicitly approve it.
