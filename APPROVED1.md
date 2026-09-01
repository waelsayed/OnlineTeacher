Approved — proceed with **Step 1: Domain Foundation only**.

Follow `AGENTS.md`, `IMPLEMENTATION_PLAN.md`, `UPDATEPLAN1.md`, and `STEP0.md` as the source of truth.

Implement only the approved Step 1 domain foundation:

* Teacher
* TeacherPlatform
* PlatformStatus
* Permission catalog/constants
* Role
* RolePermission
* TeacherPlatformMembership
* Approved ownership/membership model
* PublicId value object
* Slug value object
* `ITenantScoped`
* `IAuditable`
* Required domain relationships and invariants
* Domain-level validation and business rules that belong to these entities

Important constraints:

* `PublicId` is globally unique and non-sequential.
* `Slug` is NOT globally unique. Duplicate slugs are allowed.
* Do not add a global unique constraint or domain rule preventing duplicate slugs.
* Do not resolve platform identity by Slug alone.
* Preserve the approved `PublicId + Slug` routing behavior for the later API/routing implementation.
* Keep the domain independent from ASP.NET Core, EF Core, PostgreSQL, JWT, HTTP, or API concerns.
* Do not introduce unnecessary abstractions, generic repositories, CQRS, MediatR, domain events, or other architecture not explicitly required by the plan.

Do NOT implement Step 2 or later:

* No EF Core DbContext
* No database configurations/migrations
* No PostgreSQL implementation
* No password hashing
* No JWT/authentication
* No API endpoints
* No tenant middleware
* No application services
* No integration tests

Testing:

* Add/update only the unit tests relevant to the Step 1 domain foundation.
* Include tests for PublicId generation/validation and Slug normalization/validation.
* Include tests for PlatformStatus state rules where applicable.
* Include tests for permission/role/membership invariants where applicable.
* Do not create integration tests yet.

After implementation:

1. Run `dotnet build`.
2. Run the Step 1 unit tests.
3. Confirm there are 0 warnings and 0 errors.
4. Inspect the resulting domain structure and dependencies.
5. Update `Implementation Status` and `Decision Log`.
6. Provide a concise report of files changed, domain decisions, tests, and verification results.
7. Commit Step 1 as one logical commit.

Then **STOP**.

Do not proceed to Step 2 until I explicitly approve it.
