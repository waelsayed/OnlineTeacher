### Task 1 — Teacher Platform Management

Steps 0–8 are completed and approved.

Current baseline:

* Latest commit: `fd10cce`
* Working tree is clean.
* Architecture, authentication, authorization, tenant resolution, Dockerization, testing, and Git strategy are already established.
* Do NOT redesign the existing architecture.

Your task is to implement the first real product feature:

# Teacher Platform Management

The goal is to make the Teacher Platform manageable through authenticated APIs while strictly respecting the existing tenant, ownership, role, and permission model.

---

## 1. Read Before Coding

Read and understand:

* `AGENTS.md`
* `IMPLEMENTATION_PLAN.md`
* All existing architecture/domain/application/API documentation
* Existing Domain, Application, Infrastructure, API, UnitTests, and IntegrationTests
* Existing authentication, JWT, tenant resolution, permission authorization, and membership implementation

Do not assume missing behavior. Follow the existing documentation and domain model.

Before implementation, inspect the current code and provide a short implementation plan internally/in your report.

---

## 2. Functional Scope

Implement the minimum complete Teacher Platform Management capability required by the existing architecture.

### Platform Profile

Authenticated authorized users must be able to:

* Retrieve the current tenant/platform information.
* Update the platform's editable profile information.
* Update the platform name.
* Update the platform slug where supported by the existing domain rules.
* Preserve the existing PublicId.
* Never allow changing the internal platform ID.
* Never allow changing the PublicId through a normal management API.

Do not invent additional profile fields unless they already exist in the domain/documentation.

### Platform Status

Respect the existing platform lifecycle:

`PendingActivation → Active → Deactivated`

Existing business rules must remain authoritative.

Do not introduce new states.

Do not bypass the existing activation rules.

Do not allow an unauthorized user to activate/deactivate a platform.

If activation/deactivation management is already implemented, expose/use it correctly through the API rather than duplicating the business logic.

---

## 3. Membership & Ownership Management

Implement management of Teacher Platform memberships according to the existing model.

The platform Owner must be able to:

* View platform members.
* Add a teacher/user as a member where the existing domain model supports it.
* Assign/change the member's role where permitted.
* Remove a member where permitted.

Respect ownership rules.

### Important ownership constraints

* The platform must always have a valid owner.
* Do not allow removing the last/only owner.
* Do not accidentally create multiple owners where the domain rules prohibit it.
* Do not allow an ordinary Assistant/Admin to bypass ownership restrictions.
* Do not allow users to manage memberships outside their authorized tenant.

If the current domain model requires changes to support this cleanly, make the minimum necessary domain change and document why.

Do NOT create a generic user/identity system just for this task if the current model does not support it.

---

## 4. Roles & Permissions

Use the existing dynamic role/permission system.

Do NOT hard-code authorization decisions in controllers.

Use the existing permission mechanism such as:

`RequirePermission(...)`

where appropriate.

Management operations should have explicit permission requirements.

At minimum, distinguish between:

* Viewing platform management information.
* Updating platform information.
* Managing memberships/roles.
* Managing platform status.

Use existing permission conventions where possible.

If new permission codes are genuinely required, add them through the existing permission catalog/seeding mechanism rather than scattering strings throughout the code.

Do not redesign the authorization system.

---

## 5. Tenant Security

This is critical.

Authenticated Teacher A must NOT be able to manage Teacher B's platform merely because Teacher A can browse Teacher B's public platform.

Remember the existing architecture:

* Tenant route resolves the target platform.
* Public browsing may be cross-tenant.
* Protected management operations require membership/authorization within the resolved tenant.

Therefore:

* Teacher A accessing Teacher B's protected management API → `403`.
* Anonymous protected management request → `401`.
* Valid tenant + authorized member → allowed.
* Valid tenant + authenticated non-member → denied.
* Invalid PublicId → `404`.
* Correct PublicId + wrong slug → existing canonical `301` behavior.

Do not reintroduce global JWT-to-route tenant binding middleware.

The existing cross-tenant public browsing behavior must remain intact.

---

## 6. API Design

Follow the existing API conventions.

Use DTOs for API boundaries.

Never expose EF Core entities directly.

Use the existing Application services/use-case pattern.

Prefer one application service per meaningful use case rather than a large "PlatformManager" service.

Controllers should remain thin:

* receive request
* invoke application service
* return appropriate response

Business rules belong in the appropriate domain/application layer.

Use the existing ProblemDetails/error handling conventions.

Return appropriate HTTP status codes consistently with the existing API design.

Do not introduce a new API style.

---

## 7. Persistence

Use the existing EF Core/PostgreSQL infrastructure.

Respect:

* tenant query filters
* TenantContext
* existing relationships
* concurrency behavior
* unique constraints

Do not bypass tenant filtering casually.

Do not introduce generic repositories.

Do not introduce CQRS/event bus just for this task.

---

## 8. Validation & Business Rules

Validate all externally supplied values.

In particular:

* platform name
* slug
* member identifiers
* role identifiers
* status transitions

Reuse existing domain value objects and validation wherever possible.

Do not duplicate validation rules in multiple layers unnecessarily.

---

## 9. Tests

Add comprehensive tests for the new behavior.

### Unit tests

Cover:

* successful platform profile update
* invalid platform name/slug
* unauthorized update
* membership rules
* role assignment rules
* owner protection
* invalid state transitions where applicable
* application-level tenant mismatch behavior

### Integration tests

Using the existing PostgreSQL/Testcontainers setup, verify at minimum:

1. Authorized owner can retrieve platform management data.
2. Authorized owner can update platform profile.
3. Unauthorized member cannot perform owner-only operation.
4. Non-member authenticated teacher cannot manage another teacher's platform.
5. Teacher A cannot manage Teacher B's platform.
6. Anonymous request receives `401`.
7. Invalid PublicId receives `404`.
8. Wrong slug receives `301`.
9. Existing public cross-tenant browsing behavior is not broken.
10. Existing authentication/authorization tests remain green.

Do not weaken or remove existing tests to make the new tests pass.

Run the full test suite, not only the newly added tests.

---

## 10. Backward Compatibility

Existing behavior must continue to work.

Pay particular attention to:

* Login requiring PublicId.
* JWT claims.
* Dynamic permissions.
* Tenant route resolution.
* Canonical slug redirects.
* Public cross-tenant browsing.
* `/health`.
* Docker configuration.

Do not modify these behaviors unless the existing implementation genuinely prevents this task from working correctly.

If a modification is unavoidable, explain exactly why.

---

## 11. Documentation

Update the relevant documentation to reflect the implemented capability.

Update `IMPLEMENTATION_PLAN.md` with the task status/decision log if appropriate.

If the repository convention requires a task documentation file, create one.

Do not create unnecessary documentation files.

---

## 12. Git

Work in small logical commits if the task requires multiple independent changes.

Do not rewrite previous Git history.

Do not push to `origin`.

At the end, the working tree must be clean.

---

## 13. Strict Constraints

Do NOT:

* redesign the architecture
* replace EF Core
* introduce generic repositories
* introduce CQRS
* introduce MediatR
* introduce event sourcing
* introduce message brokers
* introduce Redis
* introduce microservices
* introduce Kubernetes
* introduce unnecessary abstractions
* change JWT architecture
* restore global JWT tenant binding
* implement Student functionality
* implement Courses
* implement Payments
* implement Media/File management
* implement Dashboard/Analytics
* add unrelated features

This task is **Teacher Platform Management only**.

---

# Completion Criteria

The task is complete only when:

* Teacher Platform management APIs are implemented.
* Existing tenant isolation remains correct.
* Existing public cross-tenant browsing remains possible.
* Authorization is permission-based and tenant-aware.
* Ownership rules are enforced.
* Validation/business rules are enforced.
* DTO boundaries are respected.
* Unit tests pass.
* Integration tests pass.
* Full `dotnet build --warnaserror` passes.
* Full `dotnet test` passes.
* No secrets are introduced.
* Working tree is clean.
* Documentation reflects the completed work.
* Changes are committed with clear commit messages.

---

# STOP CONDITION

This is **Task 1 only**.

After completing it:

1. Report exactly what was implemented.
2. List all changed files.
3. Explain any domain/architecture changes and why they were necessary.
4. List all new/changed API endpoints.
5. Report authorization/permission behavior.
6. Report tenant-isolation verification.
7. Report unit-test results.
8. Report integration-test results.
9. Report build result.
10. Report commit hash(es).
11. Report final `git status`.
12. Report any risks or remaining decisions.

Then **STOP**.

Do NOT continue to Student Identity, Following, Courses, Enrollment, Media, Dashboard, or any other future task.

Wait for my explicit review and approval.
