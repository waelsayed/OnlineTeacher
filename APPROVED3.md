Approved — proceed with **Step 3: Application Layer only**.

Follow `AGENTS.md`, `IMPLEMENTATION_PLAN.md`, `UPDATEPLAN1.md`, and the completed Step 1/Step 2 implementation as the source of truth.

Implement only these approved application use cases:

1. `RegisterTeacher`
2. `CreateTeacherPlatform`
3. `ActivateTeacherPlatform`
4. `Authenticate`
5. `ResolveTenantRoute`

### Application Layer Rules

* Keep the Application layer independent from ASP.NET Core, HTTP, controllers, and PostgreSQL-specific APIs.
* Use application interfaces/ports for infrastructure dependencies.
* Do not expose EF Core entities directly as API contracts.
* Keep business rules that belong to the Domain inside the Domain; do not duplicate them unnecessarily in Application.
* Application services/use cases should orchestrate the Domain and infrastructure ports.
* Use explicit DTOs/commands/results where appropriate.
* Do not introduce CQRS/MediatR or unnecessary abstractions unless clearly justified by the existing architecture.

### RegisterTeacher

Implement teacher registration according to the approved domain/business rules.

Requirements:

* Validate required input.
* Normalize/validate email through the existing domain value object.
* Enforce duplicate email through the database constraint and handle the resulting conflict cleanly.
* Passwords must never be stored as plaintext.
* Use the existing `IPasswordHasher<Teacher>` abstraction.
* Do not implement JWT here.

### CreateTeacherPlatform

Implement platform creation according to the approved model.

Requirements:

* Generate a cryptographically secure, non-sequential PublicId through the existing domain/value-object mechanism.
* PublicId uniqueness remains enforced by the database.
* Slug is NOT globally unique.
* Duplicate slugs are allowed.
* Create the appropriate owner membership/role relationship according to the approved Domain Model.
* Keep platform status initially `PendingActivation`.
* The operation must be transactional where required by the approved plan.
* Do not add global slug uniqueness checks.

### ActivateTeacherPlatform

Implement the approved activation workflow.

Requirements:

* Respect the existing PlatformStatus state transitions.
* Only the approved transition from `PendingActivation` to `Active`.
* Preserve `ActivatedAtUtc`.
* Invalid state transitions must produce the approved application/domain error.
* Handle concurrency according to the infrastructure/domain design already established.
* Do not add unrelated activation behavior.

### Authenticate

Implement the application-level authentication use case only.

Requirements:

* Locate the teacher by the approved identity (email).
* Verify the password through `IPasswordHasher<Teacher>`.
* Never expose or log the password/hash.
* Return the approved authentication result required by the later API/JWT layer.
* Do not implement JWT generation inside the Application layer unless the existing architecture explicitly defines a JWT abstraction/port for it.
* Do not create API authentication middleware yet.

### ResolveTenantRoute

Implement the application logic for the approved platform route resolution.

The routing behavior is fixed:

* PublicId not found → `NotFound`
* PublicId found + supplied slug equals current canonical slug → resolved successfully
* PublicId found + supplied slug differs from current canonical slug → return the canonical platform information required for a `301` redirect
* Never treat PublicId alone as a valid route when the supplied slug is wrong.
* Slug is not an identity and is not globally unique.
* Do not query/resolve a platform by Slug alone.

The actual HTTP `301` response belongs to the API layer later, not the Application layer.

### Error Handling

Use the approved application exceptions:

* NotFound
* Validation
* BusinessRuleViolation
* TenantMismatch
* Concurrency

Do not leak database-specific exception details through the Application API.

### Tenant Isolation

Respect the existing tenant model.

Important:

* Tenant resolution and authorization are separate concerns.
* Do not bypass EF Core tenant query filters.
* Do not implement authorization middleware in this step.
* Central operations must not accidentally execute under a teacher tenant context.
* Do not introduce cross-tenant access.

### Transactions

Where an operation creates multiple related records that must succeed/fail atomically, use the approved transaction approach.

In particular, ensure platform creation and its required ownership/membership records are atomic.

Do not introduce a generic Unit of Work abstraction merely for abstraction's sake if the existing infrastructure already provides the required transaction mechanism.

### Testing

Add comprehensive unit tests for the Application use cases.

At minimum cover:

RegisterTeacher:

* valid registration
* invalid input
* duplicate email
* password hashing behavior
* password is not persisted/logged as plaintext

CreateTeacherPlatform:

* valid creation
* PublicId generation
* PendingActivation status
* owner membership/role creation
* duplicate slug is allowed
* required transaction behavior

ActivateTeacherPlatform:

* PendingActivation → Active
* invalid state transition
* ActivatedAtUtc
* concurrency failure handling where applicable

Authenticate:

* valid credentials
* invalid email
* invalid password
* inactive/deactivated teacher behavior if required by the approved business rules

ResolveTenantRoute:

* invalid PublicId → NotFound
* matching PublicId + canonical slug → success
* matching PublicId + wrong slug → canonical redirect result
* duplicate slugs do not affect resolution because PublicId is the identity
* never resolve by slug alone

### Dependency Injection

Do not add API startup/DI registration unless it is strictly required to test or compose the Application layer.

The API/Infrastructure composition root belongs to the appropriate later step.

### Scope Control

Do NOT implement Step 4 or later.

Specifically, do not implement:

* API endpoints/controllers
* JWT token generation/middleware
* Tenant middleware
* Authorization handlers/attributes
* ProblemDetails middleware
* HTTP 301 responses
* Structured API logging
* Docker changes
* Integration tests
* New database migrations unless absolutely required by an existing Step 3 application requirement

### Verification

After implementation:

1. `dotnet build -warnaserror`
2. Run all unit tests.
3. Confirm existing 62+ tests remain green.
4. Verify Application has no unwanted infrastructure/framework dependencies.
5. Inspect dependency direction.
6. Update `Implementation Status` and `Decision Log`.
7. Create one logical Step 3 commit.

Report:

* Files changed
* Application use cases/services created
* Interfaces/ports introduced
* DTOs/results created
* Business decisions
* Unit test results
* Build results
* Any warnings/deviations

Then **STOP**.

Do not proceed to Step 4 until I explicitly approve it.
