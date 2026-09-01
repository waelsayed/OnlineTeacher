Approved — proceed with **Step 2: Infrastructure only**.

Follow `AGENTS.md`, `IMPLEMENTATION_PLAN.md`, `UPDATEPLAN1.md`, `STEP0.md`, and the approved Step 1 implementation as the source of truth.

Implement only the approved Infrastructure scope:

### 1. ApplicationDbContext

Create and configure `ApplicationDbContext` using EF Core with PostgreSQL/Npgsql.

Configure the existing domain entities and relationships without changing their business meaning.

### 2. Tenant Context

Implement the approved scoped `ITenantContext` mechanism required for tenant-aware EF Core access.

The tenant context must support the approved security flow:

Authentication → Tenant Resolution → Authorization → Tenant-aware Data Access

Do not treat the EF query filter as a replacement for authorization.

### 3. Tenant Query Filters

Implement EF Core global query filters for entities that implement `ITenantScoped`.

Important:

* Tenant isolation must be enforced at the data-access layer as defense in depth.
* Do not accidentally filter central/global entities that are not tenant-scoped.
* Do not bypass tenant filters through normal application queries.

### 4. Database Constraints & Indexes

Create the EF Core configuration for the approved database constraints/indexes.

Mandatory:

* `Teacher.Email` → UNIQUE
* `TeacherPlatform.PublicId` → UNIQUE
* `TeacherPlatform.Slug` → NON-UNIQUE index
* Do NOT create a unique index/constraint on `Slug`.
* Duplicate slugs must be allowed.
* Do not add any global slug uniqueness validation or concurrency protection.

Preserve the approved routing identity:
`PublicId + Slug`

PublicId remains the canonical platform identity.

### 5. Relationships & Constraints

Configure the required relationships and constraints for:

* Teacher
* TeacherPlatform
* Role
* Permission
* RolePermission
* TeacherPlatformMembership

Ensure the database model correctly represents the domain invariants already implemented in Step 1.

Use appropriate foreign keys, delete behaviors, indexes, and unique constraints where required.

Do not invent additional business rules.

### 6. Auditing / Concurrency

Implement only the auditing/concurrency infrastructure explicitly required by the approved plan.

Do not introduce unnecessary infrastructure abstractions.

### 7. Migrations

Create the initial EF Core migration for PostgreSQL.

The migration must accurately reflect the approved Domain Model and indexes.

Before finalizing the migration, inspect it carefully to ensure:

* PublicId is unique.
* Email is unique.
* Slug is NOT unique.
* No accidental unique slug index exists.
* No unexpected tables/columns/constraints were introduced.

### 8. Password Hashing

Implement the approved `PasswordHasher<Teacher>` infrastructure only if it is part of Step 2 according to the implementation plan.

Use the framework-provided password hasher.

Do NOT implement authentication, login, JWT, or API endpoints yet.

### Scope Control

Do NOT implement Step 3 or later.

Specifically, do not implement:

* Application services/use cases
* Registration service
* Platform creation service
* Platform activation service
* Authentication/login
* JWT
* API endpoints/controllers
* Tenant middleware
* Authorization handlers
* Business workflows
* Integration tests

### Testing & Verification

After implementation:

1. Run `dotnet restore` if required.
2. Run `dotnet build`.
3. Run all existing unit tests.
4. Validate the EF Core migration.
5. Start PostgreSQL through Docker Compose if required.
6. Apply the migration against the PostgreSQL container.
7. Verify the database schema/indexes.
8. Specifically verify that duplicate Slugs are permitted while duplicate PublicIds/Emails are rejected.
9. Confirm tenant query filters compile and behave as intended without introducing authorization logic.

Target:

* 0 warnings
* 0 errors
* All existing tests passing.

### Documentation & Git

Update:

* `Implementation Status`
* `Decision Log`

Create a logical Step 2 commit containing only Step 2 changes.

After completion, report:

* Files changed
* DbContext/configuration structure
* Tenant context/query-filter approach
* Database constraints/indexes
* Migration name
* Verification/test results
* Any warnings, assumptions, or deviations

Then **STOP**.

Do not proceed to Step 3 until I explicitly approve it.
