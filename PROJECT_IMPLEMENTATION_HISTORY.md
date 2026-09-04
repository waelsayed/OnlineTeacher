# Online Teacher — Project Implementation History

> This document reconstructs the Online Teacher project's implementation history from its
> beginning up to the current checkpoint (after completed Task 6 — Student Coupons).
>
> It is **evidence-based**: every important rationale is classified as one of:
>
> * **Documented decision** — explicitly recorded in project documentation or an approved planning/decision document.
> * **Implementation evidence** — clearly demonstrated by the code, tests, migrations, or Git history.
> * **Inference** — a reasonable conclusion derived from the implementation but not explicitly documented, labelled as such.
>
> It is **not** a raw commit list. Its purpose is to preserve *why* the system was built the way it
> was, so the project owner can review the architecture and challenge decisions without depending on
> terminal history or this session's memory.

---

# 1. Project Overview

## What the project is

**Online Teacher** is a SaaS educational platform developed by **ProNileSoft**. It is designed
**specifically for school teachers**, not trainers. Teachers use the platform to manage content and
interact with students; students use one central identity to interact with multiple teachers.

## Central Platform vs Teacher Platforms

The system follows a **Central Platform owned by ProNileSoft**, combined with multiple independent
**Teacher Platforms** (one per teacher/tenant):

* The **Central Platform** is responsible for teachers, teacher platforms, subscriptions, plans,
  activation, central administration, central permissions, teacher discovery, central coupons, and
  teacher-level financial operations.
* A **Teacher Platform** is responsible for students, courses, units, lessons, revision, exams,
  homework, enrollments, follow relationships, wallet, student payments, student coupons, posts,
  comments, messages, notifications, teacher team, dynamic permissions, and student-related financial
  records.

A **student** has one central identity and can interact with multiple teachers. Each Teacher Platform
represents an **independent tenant**.

## Current technology stack

* **Backend:** ASP.NET Core Web API on **.NET 10**, C#.
* **Database:** PostgreSQL (primary), run in a Docker container for local development.
* **Development environment:** Docker / Docker Compose (PostgreSQL 16 Alpine with a persistent volume).
* **ORMs / infra:** EF Core with Npgsql.
* **Auth:** JWT Bearer authentication.
* **Testing:** xUnit, FluentAssertions, Testcontainers PostgreSQL for integration tests, WebApplicationFactory.

## Current architectural style

A **pragmatic layered architecture** with clear separation between **Domain**, **Application**,
**Infrastructure** (persistence), and **API/Presentation**. Boundaries are enforced by dependency
direction and by the project's documented architecture. The system deliberately avoids
over-engineering: no generic repositories, no CQRS/MediatR, no event bus, no microservices, and no
distributed systems.

---

# 2. Architectural Principles

The following principles were established throughout the project.

## Central Platform vs Teacher Platform separation

* **What it means:** Features, data, and authorization are cleanly separated into a Central area and a
  Teacher Platform (tenant) area.
* **Why:** The product owns a central identity and platform administration, but each teacher operates
  an independent tenant.
* **How:** Central operations live under `/api/central/...`; tenant operations live under
  `/{publicId}/{slug}/api/...`. Central entities (Teacher, TeacherPlatform, Permission) are not tenant
  filtered; tenant entities (Role, RolePermission, TeacherPlatformMembership, Course, Unit, Lesson)
  are tenant-scoped.
* **Security/business implications:** Tenant-scoped data cannot leak across tenants; central identity
  data remains global. A tenant cannot manage another tenant.

## Multi-tenancy & tenant isolation

* **What it means:** Each Teacher Platform is a tenant; tenant-owned data must only be accessed within
  the correct tenant context.
* **Why:** Prevents `Teacher A → accessing Teacher B data`.
* **How:** A scoped `TenantContext` is established by `TenantRouteMiddleware` from `{publicId}/{slug}`.
  EF Core global query filters are applied only to `ITenantScoped` entities (a data-layer defense).
  Application services additionally enforce membership/ownership via `PlatformAccessGuard` /
  `RequireMemberAsync`.
* **Security/business implications:** Defense in depth — even if an application bug forgets a guard,
  the EF filter blocks cross-tenant reads at the data layer.

## Student central identity

* **What it means:** A student has one central identity usable across all Teacher Platforms; no
  per-tenant student accounts.
* **Why:** A student interacts with multiple teachers through one account (documented).
* **How:** `Student` and `StudentFollow` are **central, non-tenant-scoped** entities under `/api/student/...`.
* **Business implications:** Following and (future) enrollment are resolved through the central
  student identity while remaining bound to the correct tenant/course context.

## Teacher Platform ownership & dynamic permissions

* **What it means:** Platform ownership is represented via a membership + Owner role; authorization
  uses Role + Permission, not many specialized roles.
* **Why:** Avoids dozens of specialized roles (e.g. not `PaymentAssistant`/`ExamAssistant`). An
  Assistant may hold exactly the permissions granted.
* **How:** `Role` + `RolePermission` + `Permission` catalogs; `[RequirePermission("...")]` maps to a
  dynamic policy and handler.
* **Business implications:** Permission is enforced at the API/resource boundary, not just by hiding
  buttons.

## PublicId + Slug platform routing

* **What it means:** Teacher Platforms have a stable, non-sequential `PublicId` plus a `Slug`.
* **Why:** Public URLs must not expose sequential DB IDs, and slugs change over time.
* **How:** `PublicId` is a cryptographically generated 12-char base62 value (globally unique); `Slug`
  is a canonical URL component and is **NOT** globally unique. Routing validates both:
  invalid PublicId → 404; correct PublicId + wrong slug → 301 to the canonical URL; correct pair → resolve.
* **Business implications:** Slug is identity-adjacent but not identity; PublicId is the canonical identity.

## JWT authentication

* **What it means:** Bearer JWT authentication with issuer/audience/signing-key validation and a
  configured `KeyId`.
* **Why:** Stateless, standards-based authentication suitable for an API.
* **How:** `JwtTokenFactory` issues tokens; bearer validation resolves from the same `JwtOptions`.

## Principal type separation

* **What it means:** The JWT distinguishes the principal type: `teacher` vs `student`.
* **Why:** A student must never be treated as a teacher (and vice versa) even though both use JWT.
* **How:** `principal_type` claim, `PrincipalTypeRequirement`/`PrincipalTypeHandler`, and
  `[RequirePrincipalType("...")]`.
* **Business implications:** Student JWT cannot satisfy teacher-management endpoints; teacher JWT
  cannot satisfy student-only endpoints.

## DTO API boundary

* **What it means:** Persistence/domain entities are never exposed directly through the API.
* **Why:** Decouples the API contract from internal implementation and protects sensitive fields.
* **How:** Request/response DTOs and application-layer result/DTO types.

## EF query filters as defense in depth

* **What it means:** Application-level authorization is the primary gate; EF tenant filters are a
  secondary data-layer guard.
* **Why:** Tenant isolation must not depend on every developer remembering to add filters.
* **How:** Global query filters on `ITenantScoped` entities only; central entities are not filtered.

## Application-level tenant/membership checks

* **What it means:** Even framework-gated endpoints re-verify tenant membership/ownership in the
  application layer.
* **Why:** Defense in depth beyond permission policies; prevents a valid cross-tenant JWT from
  managing another teacher's data.
* **How:** `PlatformAccessGuard.RequireMemberAsync` / `RequireOwnerAsync` throw `TenantMismatchException` (403).

## Provider abstractions

* **What it means:** External providers (password hashing today) sit behind ports so the domain does
  not depend on a specific implementation.
* **Why:** Keeps the domain independent and testable.
* **How:** `IPasswordHasher` implemented by ASP.NET Core's `PasswordHasher<Teacher>` in Infrastructure.

## Structured logging

* **What it means:** Built-in ASP.NET Core structured JSON console logging.
* **Why:** Environment-aware, suitable for Docker log collection without extra dependencies.
* **How:** `AddJsonConsole`; no Serilog unless a concrete requirement emerges.

## ProblemDetails / consistent error handling

* **What it means:** All errors map to consistent RFC 7807 `ProblemDetails` responses.
* **Why:** Clients need consistent, non-leaky errors.
* **How:** `ExceptionHandlingMiddleware` maps `ValidationException`→400, `NotFoundException`→404,
  `DuplicateEmailException`→409, `BusinessRuleViolationException`→422, `TenantMismatchException`→403,
  `ConcurrencyException`→409, auth→401, generic→500 (dev-only detail).

## Audit trail & session/station tracking (guidance)

* **What it means:** Important actions should be auditable (actor, tenant, action, entity, id,
  timestamp, session/station, IP, changes). Audit records are historical and not silently deleted.
* **Why:** Required for financial and business accountability.
* **How:** `IAuditable` exists on entities; fully built-out audit/session infrastructure is a later
  concern. This is partially implemented (documented intent).

## Testing strategy

* **What it means:** Layered automated tests: domain unit tests, application service tests,
  authorization tests, and integration tests against a real PostgreSQL Testcontainer.
* **Why:** Critical behavior (especially tenant isolation and state transitions) must be verified.
* **How:** xUnit + FluentAssertions + Testcontainers PostgreSQL + WebApplicationFactory.

---

# 3. Implementation Timeline

The project progressed through a scaffolding phase (Steps 0–8) followed by product feature tasks
(Tasks 1–6), and now pauses after Task 6.

Chronological order:

```
Step 0  — Scaffolding & Tooling
Step 1  — Domain Foundation
Step 2  — Infrastructure (EF Core, tenant context, migration)
Step 3  — Application Layer
Step 4  — API & Composition Layer (+ JWT, auth, tenant routing)
  [SECURITYFIX — JWT tenant binding, then FINALSTEP4 — reverted to allow public browsing]
Step 5  — Dockerization & Deployment Hardening
Step 6  — Testing (authorization/claim construction + state transitions)
Step 7  — Verification (full containerized smoke test)
Step 8  — Git strategy / final repository cleanup
Task 1  — Teacher Platform Management
Task 2  — Central Student Identity & Following
Task 3  — Teacher Platform Course Content
Task 4  — Student Enrollment in Teacher Courses
Task 5  — Student Wallet & Course Purchase
Task 6  — Student Coupons (Teacher Platform Coupons)
```

---

# 4. For Every Step/Task, "WHY"

## Step 0 — Scaffolding & Tooling

* **Problem:** No solution, project structure, or development environment existed.
* **Goal:** Create the minimum .NET 10 solution, project separation, Docker PostgreSQL setup, and
  `.gitignore`, `.env.example`, `docker-compose.yml`.
* **Design:** Layered project separation (API / Application / Domain / Infrastructure / Tests),
  PostgreSQL 16 Alpine in Docker with a persistent volume and healthcheck.
* **Why this design:** Follows `AGENTS.md`'s fixed stack (ASP.NET Core Web API, .NET 10, PostgreSQL,
  Docker) and the documented layered architecture. Keep it minimal — no extra projects/features.
* **Implementation evidence:** Solution format `.slnx` (SDK 10 default); pinned package versions for
  .NET 10.
* **Verification:** `dotnet restore`, `dotnet build`, Docker Compose config validation, PostgreSQL
  healthy.
* **Result:** A reproducible scaffold ready for the domain in Step 1.

## Step 1 — Domain Foundation

* **Problem:** No domain model existed for teachers, platforms, roles, and permissions.
* **Goal:** Implement the domain objects for the first vertical slice.
* **Design:** `Teacher`, `TeacherPlatform`, `PlatformStatus`, `Permission`, `Role`, `RolePermission`,
  `TeacherPlatformMembership`; value objects `PublicId`, `Slug`, `Email`; markers `ITenantScoped`,
  `IAuditable`. Ownership is represented through a membership with an Owner role + `IsOwner` flag.
* **Why this design:** The domain must be independent of ASP.NET/EF/PostgreSQL/JWT. `PublicId` is
  globally unique and non-sequential; `Slug` is **not** globally unique. No specialized assistant roles.
* **Implementation evidence:** Domain has zero external dependencies.
* **Verification:** Unit tests (PublicId, Slug, PlatformStatus, permission/role/membership invariants);
  0 warnings / 0 errors.
* **Result:** A clean, framework-independent domain foundation.

## Step 2 — Infrastructure

* **Problem:** No persistence, no tenant context, no migrations.
* **Goal:** Implement `ApplicationDbContext`, `ITenantContext`/`TenantContext`, EF configurations,
  tenant query filters, and the `InitialCreate` migration.
* **Design:** Scoped `TenantContext`; global query filters **only** on tenant-scoped entities; unique
  `Teacher.Email`, unique `TeacherPlatform.PublicId`, **non-unique** `Slug` index. Password hashing
  via `IPasswordHasher` port.
* **Why this design:** Tenant isolation as a data-layer defense; query filters must not be the
  authorization mechanism. PublicId/Email unique, Slug non-unique per approved rule.
* **Implementation evidence:** EF Core unified to 10.0.11 across the graph (MSB3277 fix); value
  converters exposed as named static fields; design-time factory for migrations.
* **Problems discovered:** Assembly conflicts (version alignment); value-converter name shadowing —
  both fixed by explicit naming/version pinning.
* **Verification:** `InitialCreate` migration applied to Docker PostgreSQL; duplicate slug allowed,
  duplicate PublicId/Email rejected.
* **Result:** Persistent, tenant-aware infrastructure.

## Step 3 — Application Layer

* **Problem:** No use cases or business orchestration.
* **Goal:** Implement `RegisterTeacher`, `CreateTeacherPlatform`, `ActivateTeacherPlatform`,
  `Authenticate`, `ResolveTenantRoute`.
* **Design:** One service per use case; purpose-specific persistence ports; `IUnitOfWork` (single
  `SaveChangesAsync` commit); atomic platform creation; domain state guards for status transitions.
* **Why this design:** Small focused services with explicit dependencies; avoid a generic UoW; keep
  registration/creation/activation atomic; keep Application independent from infrastructure.
* **Implementation evidence:** Application project has zero package references (dependency direction
  Application → Domain only).
* **Decisions:** Duplicate email enforced by the DB unique constraint and translated at the
  persistence boundary; duplicate/non-unique slugs allowed with no uniqueness check; authentication
  returns a generic failure for unknown email/wrong password; a dedicated optimistic concurrency
  token intentionally deferred (documented risk).
* **Verification:** Comprehensive unit tests; existing 62+ tests green; 0/0 build.
* **Result:** A working application layer orchestrating the domain.

## Step 4 — API & Composition Layer (+ JWT, auth, tenant routing)

* **Problem:** No HTTP surface, no authentication/authorization, no tenant routing.
* **Goal:** Compose all layers and expose the approved endpoints.
* **Design:** `Program.cs` composition root; JWT bearer auth; dynamic permission policies
  (`RequirePermission`); `TenantRouteMiddleware` for `/{publicId}/{slug}`; `ExceptionHandlingMiddleware`
  for ProblemDetails; permission seeding via `PermissionSeeder`.
* **Why this design:** Expose RESTful endpoints; keep authentication and authorization separate;
  enforce permissions server-side from trusted claims.
* **Implementation evidence:** JWT claims (sub, tenant, roles, permissions, isOwner); `KeyId` handled
  for signature validation; ProblemDetails mapping.
* **Security evolution (SECURITYFIX → FINALSTEP4):** An initial middleware-level JWT-tenant binding
  was added (`SECURITYFIX`) that rejected any authenticated request whose `tenant` claim differed from
  the route publicId. This was **reversed/refined** (`FINALSTEP4`) because the product MUST allow an
  authenticated user to browse another tenant's **public** content. The refined middleware resolves
  the tenant but does **not** reject solely on a tenant mismatch; protected endpoints enforce tenant
  access via permission policies + membership guards.
* **Verification:** Integration tests (register → create → activate → login → access me; isolation;
  401/403/404/301; duplicate email 409). Unit 135/135, integration 14/14.
* **Result:** A complete, security-aware vertical slice.

## Step 5 — Dockerization & Deployment Hardening

* **Problem:** The API could not run reliably inside Docker.
* **Goal:** Multi-stage Dockerfile, production runtime image, Compose running API + PostgreSQL,
  environment-driven config, health endpoint.
* **Design:** Multi-stage Dockerfile (sdk:10.0 → aspnet:10.0), compose `postgres` + `api` services,
  `/health` endpoint, environment-driven connection/JWT config.
* **Why this design:** Explicit Step 5 requirement; reproducible containerized execution with no
  committed secrets.
* **Verification:** Full containerized register → create → login → activate → `/me` flow; wrong
  tenant 403; wrong slug 301; invalid PublicId 404.
* **Result:** A hardened, containerized backend.

## Step 6 — Testing

* **Problem:** Some authorization and claim-construction behavior was only tested end-to-end.
* **Goal:** Fill the gap with focused unit tests plus a missing integration scenario.
* **Design:** `JwtTokenFactoryTests`, `PermissionAuthorizationTests`; an integration test proving an
  already-active platform returns 422.
* **Why this design:** Assert exact server-issued claim construction and permission policy behavior in
  isolation (Section 19 gap).
* **Verification:** Build 0/0; unit 135/135; integration 14/14.
* **Result:** Stronger authorization/claim coverage.

## Step 7 — Verification (full containerized smoke test)

* **Problem:** Need proof the whole system works from a clean Docker environment.
* **Goal:** Run the end-to-end flow and authorization/routing scenarios against the containerized API.
* **Design/How:** `docker compose up -d --build`; exercise register/create/login/activate/me;
  verify 403/301/404/401 and `/health`.
* **Why this design:** Verification-only step; no production changes.
* **Verification:** All scenarios passed; EF tenant filters confirmed in logs; no 5xx; environment torn
  down.
* **Result:** Confirmed end-to-end functionality.

## Step 8 — Git strategy / final repository cleanup

* **Problem:** Need a clean, professional Git state.
* **Goal:** Review history, `.gitignore`, secrets, and generated artifacts.
* **Design/How:** Review commits; verify no secrets/temp artifacts tracked; confirm history is clean.
* **Why this design:** Do not rewrite history for cosmetic reasons; keep hashes stable.
* **Verification:** History preserved as-is (`fd10cce`); no unwanted files; build 0/0 and full tests green.
* **Result:** Clean, reviewable repository.

## Task 1 — Teacher Platform Management

* **Problem:** The platform must be manageable through authenticated APIs.
* **Goal:** Profile get/update, membership list, add member, change role, remove member — respecting
  tenant/ownership/permission models.
* **Design:** Endpoints under `/{publicId}/{slug}/api/platform/...`; new permission `Platform.Membership`;
  new role `Assistant`; Owner-only membership mutations; last-Owner protection.
* **Why this design:** Reuses dynamic permission model; adds the minimum new permission/role; enforces
  ownership at the application layer (defense in depth).
* **Problems discovered:** Testcontainers returned the container's internal port on this Docker Desktop
  host — fixed by pinning an explicit host port (5433) via `WithPortBinding`.
* **Verification:** Unit 174/174; integration 26/26; build 0/0.
* **Result:** A manageable Teacher Platform.

## Task 2 — Central Student Identity & Following

* **Problem:** No student identity or student–teacher relationship.
* **Goal:** Central Student registration/login/profile and Follow/Unfollow/List/Is-following.
* **Design:** Central non-tenant-scoped `Student` and `StudentFollow` (an approved decision: NO Student
  PublicId); student JWT with `principal_type=student`; teacher tokens gain `principal_type=teacher`;
  follow resolves a platform PublicId → Owner teacher.
* **Why this design:** Student identity must be central across platforms; principal type separation
  prevents student/teacher confusion; following ≠ enrollment.
* **Implementation evidence:** `ux_students_email` unique; `ux_follows_student_teacher` unique (DB
  backstop against duplicate follows); student login does not require a PublicId; student JWT carries
  no permission claims (cannot manage platforms).
* **Verification:** Unit 221/221; integration 39/39; build 0/0.
* **Result:** Central student identity + following with strong isolation.

## Task 3 — Teacher Platform Course Content

* **Problem:** No tenant-scoped educational content structure.
* **Goal:** Implement Course → Unit → Lesson with explicit ordering and lifecycle.
* **Design:** Tenant-scoped `Course` (Draft/Published), `Unit`, `Lesson`; explicit 1-based contiguous
  `Position` per parent; permissions `Course.View` (read) and `Course.Manage` (mutate); no Course
  PublicId/Slug (internal Guid); duplicate course titles allowed; hard delete/cascade (no enrollment yet).
* **Why this design:** Foundational content structure for future Enrollment; minimal lifecycle; no
  invented public course URLs; pragmatic deletion given no student references exist.
* **Problems discovered:**
  * New Unit/Lesson children were tracked as `Modified` by EF relationship fixup → explicit
    `AddUnit`/`AddLesson` in the repository fixed it.
  * Reordering under an immediate-unique position index caused an EF "circular dependency" conflict →
    the approved resolution (`Tasks/Approved1.md`) enforces ordering invariants **in the domain
    aggregate only** (single writer), keeping DB position indexes non-unique.
  * Include-collection ordering was undefined → the repository orders Units/Lessons by Position on read.
* **Decision (approved deviation):** DB-level unique constraints on `(CourseId, Position)` and
  `(UnitId, Position)` were intentionally omitted because EF's change-tracking/topological ordering
  conflicts with atomic reordering. Ordering is enforced by the Course/Unit aggregates as the single
  writers of ordering state. No deferrable constraints, hand-managed SQL, or snapshot hacks.
* **Verification:** Unit 281/281; integration 51/51; build 0/0; migration applied cleanly.
* **Result:** A tenant-scoped, ordered course content hierarchy.

## Task 4 — Student Enrollment in Teacher Courses (Completed)

* **Problem:** No academic relationship connecting a central Student to a Teacher Platform Course.
* **Goal:** Implement `Student → Course` Enrollment with a tenant-scoped Enrollment, a status lifecycle,
  an `Enrollment.View` permission for read-only course-enrollment access, and student-perspective
  enrollment endpoints while preserving the central Student identity.
* **Design:**
  * Domain `Enrollment` entity (tenant-scoped) with `EnrollmentStatus` lifecycle:
    `Active` (Enrolled) → `Cancelled` (terminal).
  * Add `Enrollment.View` permission to the dynamic permission catalog.
  * Persistence via `ApplicationDbContext` DbSet + tenant query filter, an EF `EnrollmentConfiguration`,
    `EnrollmentRepository`, and the `20260903171426_AddEnrollment` migration.
  * Four application services: Enroll, ListStudent, Cancel, ListCourse.
  * API: three student-perspective endpoints (enroll / list / cancel by central student identity) and one
    teacher-side read endpoint (`GET .../courses/{courseId}/enrollments`) guarded by `Enrollment.View` +
    membership.
  * DB unique index `ux_enrollments_student_course` prevents duplicate enrollments; `Enrollment → Course`
    FK uses **Restrict** so cancel/complete statuses preserve academic records (course content cascade is
    unchanged because the Restrict FK is not referenced by Unit/Lesson children).
* **Why this design:** Enrollment is the core academic relationship between Student and Course. Keeping the
  Student central while scoping the Enrollment to the tenant preserves the documented identity model. The
  `Enrollment.View` permission follows the Course.View precedent for read access. Enrollment remains
  **distinct from Following** — following a Teacher does not enroll the student, and enrolling does not
  auto-follow (documented below).
* **Problems discovered:**
  * `EnrollmentRepository` LINQ query (ordering before a `Status.ToString()` projection) did not translate
    to SQL → fixed via a two-step materialization (order rows in SQL, then project `Status.ToString()` in
    memory).
  * An integration test initially passed an invalid-format PublicId for an unknown platform and expected a
    404 → corrected to a valid 12-char PublicId (`PublicId.Generate().Value`).
* **Decision (enrollment ≠ following):** Following and Enrollment are intentionally separate. Enrollment in a
  course creates the Student→Teacher relationship only in the sense of course membership; it does **not**
  auto-create a Follow, and the student is not required to follow to enroll. This is a deliberate,
  documented design choice within the approved Task 4 scope.
* **Verification:** Unit 315/315; integration 64/64; build `--warnaserror` 0 warnings / 0 errors.
* **Result:** A tenant-scoped, lifecycle-managed academic relationship between central Students and Teacher
  Platform Courses, with read-only teacher visibility and no payment/wallet/coupon/completion nonsense
  (those remain out of scope).

## Task 5 — Student Wallet & Course Purchase

* **Problem:** Paid course content could not be monetized: there was no Course pricing, no student wallet,
  and no way for a student to pay for and be enrolled in a Paid course with atomic, auditable financial
  records.
* **Goal:** Introduce explicit Free/Paid course pricing, a tenant-scoped student wallet, a wallet-credit
  flow (transfer submit/approve/reject), and an atomic course-purchase flow, while preserving the existing
  Free-course direct-enrollment path and all tenant-isolation guarantees.
* **Design:** `Course.SetPricing` (explicit `CoursePricingType` Free/Paid + EGP `Price`); tenant-scoped
  `StudentWallet` (lazy-created, balance derived from `FinancialTransaction` history — the balance is never
  a standalone mutable source of truth); `TransferRequest` (submit → approve/reject) for wallet credit;
  `PurchaseCourseService` performing the whole paid flow atomically (validate paid+published+balance,
  re-check duplicate active enrollment, debit wallet, create Enrollment, record FinancialTransaction) in one
  `IUnitOfWork` transaction. Wallet operations require the new `Wallet.Manage` permission on the teacher
  side; student operations are `[RequirePrincipalType("student")]`.
* **Why this design:** Financial operations must be transactional and auditable per `AGENTS.md` §15–§16
  (prevent negative balances, double spending, duplicate enrollment, double coupon/transfer consumption).
  The wallet belongs to the Teacher Platform (tenant-scoped), not the Central Platform. Free vs Paid is an
  explicit state, never inferred from `Price == null/0`. Multiple active-purchase/idempotency protection
  uses DB constraints plus application checks.
* **Problems discovered:**
  * `[Required]` attributes on the `SubmitTransferRequest` contract surfaced as model-state 400/500 errors
    in the API layer (the controller validation ran before the application service) — removed so validation
    is owned by the application service (which already validates the same rules).
  * A cross-tenant transfer review returned a `TenantMismatchException` that mapped 403 despite the transfer
    being resolved as NotFound in another tenant — resolved so a cross-tenant review returns **404**
    (NotFound), matching the "unknown resource in this tenant" semantics.
  * An empty student wallet queried by a different student returned **204 No Content** (via `Ok(null)`)
    rather than 200 with an empty body — the integration test was updated to assert the 204.
  * **Re-enrollment after cancellation** was impossible because the Task 4 `ux_enrollments_student_course`
    index was a FULL unique index on `(student_id, course_id)`, so a cancelled enrollment permanently
    blocked re-enrollment. Per the approved decision (AGENTS.md §30 surfaced to the user), the index was
    converted to a **partial unique index** (`WHERE status = Active`) so only one Active enrollment can
    exist at a time while terminal (cancelled) history may coexist.
* **Decision (re-enrollment after terminal cancellation):** The previously-approved Task 4 full unique
  constraint was replaced by a partial unique index allowing one Active enrollment per (student, course).
  This is an explicit, user-approved change surfaced through the AGENTS.md §30 process.
* **Decision (wallet ownership/derivation):** Student wallets are owned by the Teacher Platform (tenant) and
  their balance is derived from an immutable `FinancialTransaction` record set; `Refund` and `CouponCredit`
  transaction types are reserved for future Tasks (not implemented here).
* **Verification:** Unit 387/387; integration 77/77; build `--warnaserror` 0 warnings / 0 errors; the
  re-enrollment migration was applied to the dev Docker PostgreSQL.

## Task 6 — Student Coupons (Teacher Platform Coupons)

* **Problem:** Paid course purchases could not be discounted. There was no way for a Teacher Platform to issue a
  single-use, student-specific discount coupon that is applied atomically during a course purchase.
* **Goal:** Introduce a tenant-scoped, single-use `StudentCoupon` (Percentage or Fixed discount, expiring,
  assigned to one student, valid for exactly one Course) that a student applies during the `PurchaseCourseService`
  flow to reduce the final amount, alongside teacher-side coupon management (create/list/get/revoke) gated by a
  new `Coupon.Manage` permission.
* **Design:**
  * Domain `StudentCoupon` entity with `DiscountType` (Percentage/Fixed), `CouponStatus` (Active/Consumed/Expired)
    lifecycle, `ExpiresAt`, `AssignedToStudentId`, a required `CourseId`, discount calculation capped so the final
    amount never goes below zero, and terminal `Consume`/`Revoke` transitions.
  * `PurchaseCourseService` now accepts an optional `couponCode`: it locks the coupon row with
    `SELECT ... FOR UPDATE`, validates it, applies the discount, debits the wallet by the reduced final amount,
    consumes the coupon, creates the Enrollment, and records Purchase + CouponCredit `FinancialTransaction` rows
    — all inside **one explicit database transaction** so concurrency cannot double-consume a coupon.
  * `CouponCredit` is **informational/audit only**; it records the value covered by the coupon without changing
    the wallet balance. A 100% discount produces no zero-amount `Purchase` transaction, only a `CouponCredit`.
  * Teacher-side coupon CRUD endpoints under `/{publicId}/{slug}/api/platform/coupons` (create/list/get/revoke),
    gated by the new `Coupon.Manage` permission + tenant membership. Student purchase applies the coupon via
    `POST /api/student/purchase/{publicId}/{courseId}` with an optional body `{ couponCode }`.
* **Why this design:** Follows the approved Task 6 decisions (`Tasks/TASK6-1.md`, `TASK6-2.md`): single student,
  tenant-scoped, single-use and permanently terminal, Percentage 1–100% + Fixed capped, **every `StudentCoupon` is
  tied to exactly one specific Course (`CourseId` required)** — the earlier "no CourseId / all Paid Courses"
  recommendation was superseded. The purchase integration reuses the existing `IUnitOfWork`/repository
  architecture rather than introducing a parallel purchasing path. Coupon consumption must be atomic with
  wallet debit, enrollment, and financial records per `AGENTS.md` §15–§16.
* **Concurrency correction (final review):** An earlier review identified that the coupon `SELECT ... FOR UPDATE`
  was not guaranteed to hold the lock until commit. This phase added the smallest appropriate transaction
  abstraction — `IUnitOfWork.ExecuteInTransactionAsync(...)` — implemented by opening an explicit
  `IDbContextTransaction` through the EF execution strategy (preserving retry behavior) and running the whole
  purchase inside it. The coupon row lock is now held until the final `SaveChanges` + `COMMIT`, so a concurrent
  purchase of the same coupon blocks, then observes the Consumed state and fails with a business-rule violation.
* **Problems discovered:**
  * An initial integration-test design set a tenant context on the service before `PurchaseAsync`, but the
    purchase is a **central** operation that switches tenant internally; this surfaced as `TenantMismatchException`
    ("A central operation cannot run under a teacher tenant context"). Fixed so each service under test starts with
    a null (central) tenant scope exactly like the real API request, letting `PurchaseCourseService` switch tenant
    internally.
* **Decision (Course applicability correction):** Originally planned as "coupon applies to all Paid Courses (no
  CourseId)". The final review approved **one coupon = one specific Course** (`CourseId` required, stored on the
  coupon, enforced by domain invariant `CourseId == courseId` and by the purchase service). This supersedes the
  earlier planning decision and is reflected across the Task 6 documentation.
* **Decision (CouponCredit semantics):** informational/audit-only — never credits the wallet; a 100% discount
  enrolls without a Purchase debit transaction (`ConsumedInTransactionId` references the CouponCredit transaction
  in that case). Refunds remain deferred to Task 7.
* **Verification:** Unit 451/451; integration 90/90 (including a new real-concurrency test that fires two
  genuinely concurrent purchases against the same coupon on separate DB connections and asserts exactly one
  succeeds, one enrollment, one consumption, one wallet debit, no duplicate financial transactions);
  build `--warnaserror` 0 warnings / 0 errors; no Task 5 regressions.

---

# 5. Domain Evolution

The domain grew in three phases.

## Initial domain (Steps 1–4)

```
Teacher
    ↓
TeacherPlatform (Status: PendingActivation → Active → Deactivated)
    ↓
TeacherPlatformMembership (Owner role + IsOwner, or Assistant)
    ↓
Role
    ↓
RolePermission
    ↓
Permission
```

* `Teacher` — a person operating a platform; identified by email.
* `TeacherPlatform` — a tenant with a unique non-sequential `PublicId` and a non-unique `Slug`.
* `Membership` — who belongs to a platform and in what role (ownership represented through the model).
* `Role` + `RolePermission` + `Permission` — the dynamic authorization model.
* Ownership is represented through a membership with an Owner role + `IsOwner` flag.

## Added in Task 2

```
Student (central, non-tenant-scoped; NO PublicId)
    ↓
Teacher (central)
```

* `Student` — one central account per person.
* `StudentFollow` — central relationship: `Student → Teacher` (unique `(StudentId, TeacherId)`).
* Note: `Student`/`StudentFollow` are **not** tenant-scoped and are **not** under tenant query filters.

## Added in Task 3

```
Teacher Platform
    ↓
Course (tenant-scoped; Draft/Published)
    ↓
Unit (tenant-scoped; CourseId + Position)
    ↓
Lesson (tenant-scoped; UnitId + Position)
```

* `Course`, `Unit`, `Lesson` are **tenant-scoped** (`ITenantScoped`) and under the tenant query filter.
* Units/Lessons carry an explicit integer `Position` (1-based, contiguous, unique within the parent),
  enforced by the domain aggregate (single writer).

## Added in Task 4

```
Teacher Platform
    ↓
Enrollment (tenant-scoped; StudentId + CourseId + Status)
    ↓ (academic relationship to)
Student (central)
```

* `Enrollment` is **tenant-scoped** (`ITenantScoped`) and under the tenant query filter, linking a central
  `Student` to a tenant `Course`.
* `EnrollmentStatus` lifecycle: `Active` (Enrolled) → `Cancelled` (terminal).
* Duplicate enrollments are prevented at the DB level by `ux_enrollments_student_course`.
* `Enrollment → Course` FK is **Restrict** (no cascade) so cancelled/completed enrollments preserve their
  historical academic records.
* New permission: `Enrollment.View` (read access to a course's enrollments).
* **Enrollment ≠ Following:** a student may be enrolled in a Course without following its Teacher, and
  vice-versa. The two central `StudentFollow` and tenant `Enrollment` relationships are independent.

## Added in Task 5

```
Teacher Platform
    ↓
StudentWallet (tenant-scoped; lazy-created; balance derived from FinancialTransactions)
    ↓
FinancialTransaction (tenant-scoped; immutable audit record: type/direction/status/amount)
    ↓
TransferRequest (tenant-scoped; wallet-credit flow: Pending → Approved/Rejected)
Course (gains CoursePricingType Free/Paid + Price in EGP via SetPricing)
```

* `StudentWallet`, `FinancialTransaction`, and `TransferRequest` are **tenant-scoped** (`ITenantScoped`)
  and under the tenant query filter.
* The wallet **balance is derived** from the transaction history — it is not a standalone mutable source of
  truth, satisfying the "financial records must never depend only on a mutable balance field" rule.
* `Refund` and `CouponCredit` `TransactionType` values are reserved only (future Tasks); no refund/coupon
  flow is implemented in Task 5.
* The `Enrollment` unique index became **partial** (`WHERE status = Active`), enabling re-enrollment after a
  terminal cancellation while preserving history.

## Added in Task 6

```
Teacher Platform
    ↓
StudentCoupon (tenant-scoped; AssignedToStudentId + CourseId + DiscountType/Value + ExpiresAt + Status)
    ↓ (applied during purchase to reduce a Paid Course price)
StudentWallet -> FinancialTransaction (Purchase/CouponCredit)
```

* `StudentCoupon` is **tenant-scoped** (`ITenantScoped`) and under the tenant query filter.
* Coupon rules (documented): single student, single-use, expiring, non-transferable, permanently terminal after
  consumption, and **tied to exactly one specific Course** (`CourseId` required).
* `DiscountType` (Percentage 1–100% incl. 100%, Fixed capped at zero) and `CouponStatus` (Active/Consumed/Expired).
* New permission: `Coupon.Manage` (teacher-side coupon management).
* `CouponCredit` `FinancialTransaction` type is now used as an informational/audit-only record (does not change
  wallet balance); `Refund` remains reserved for a future Task.

**Relationship summary for a reader:** A Teacher owns a Platform (a tenant). The Platform owns Courses.
Courses contain ordered Units. Units contain ordered Lessons. A Student (central identity) may follow
Teachers (central `StudentFollow`) and may be enrolled in a tenant's Courses (tenant `Enrollment`). A
tenant-scoped `StudentWallet` (with its `FinancialTransaction` history and `TransferRequest` credit flows)
serves the payments/purchase path for Paid Courses, where a tenant-scoped `StudentCoupon` may be applied
to reduce the price at purchase.

---

# 6. Authentication and Authorization Evolution

## Initial model (Steps 1–4)

* `POST /api/auth/login` requires `Email + Password + PublicId` (the teacher selects their platform/tenant).
* JWT claims: `sub` = TeacherId, `tenant` = Platform PublicId, `roles`, `permissions`, `isOwner`.
* Authorization is dynamic: `[RequirePermission("...")]` maps to a dynamic policy; the handler trusts
  only server-issued permission claims.

## Security refinement (SECURITYFIX → FINALSTEP4)

* An initial middleware binding rejected any authenticated request whose JWT tenant differed from the
  route tenant. This was reversed because the product must allow an authenticated user to browse
  another tenant's **public** content.
* Final behavior: `TenantRouteMiddleware` resolves the tenant from `{publicId}/{slug}` and sets the
  `TenantContext` but does **not** reject a request solely because the JWT tenant differs. Protected
  endpoints enforce tenant access via permission policies + membership guards (application security),
  keeping defense in depth.
* **Why global JWT tenant binding was deliberately NOT used (documented, FINALSTEP4):** cross-tenant
  public browsing must be allowed; the JWT is conceptually per-principal, not per-route.

## Task 2 addition — principal type

* Teacher tokens now carry `principal_type=teacher`; student tokens carry `principal_type=student`.
* `PrincipalTypeRequirement`/`PrincipalTypeHandler` + `[RequirePrincipalType("...")]` distinguish
  teacher vs student endpoints.
* Student JWT: `sub` = studentId, `principal_type=student`, and carries **no** tenant/permission/role
  claims — so it can never satisfy teacher-management endpoints.
* Teacher JWT: retains all existing claims plus `principal_type=teacher`.

## Practical security model

* Authentication answers "who are you?" (JWT, principal type).
* Tenant resolution answers "which tenant context is this request in?" (route PublicId + slug).
* Authorization answers "may this principal perform this action here?" (permissions + membership).
* Data access adds an EF tenant filter as a final guard.

---

# 7. Tenant Isolation Evolution

## TenantContext

* A scoped `TenantContext` (`ITenantContext`) holds the resolved tenant for the current DI scope.
* `ApplicationDbContext` reads `TenantId` from this context.

## TenantRouteMiddleware

* Processes routes carrying `{publicId} + {slug}`.
* Invalid PublicId → 404; wrong slug → 301 to the canonical URL (preserving the endpoint suffix); matching
  slug → establishes the TenantContext.
* It does **not** reject authenticated cross-tenant requests solely on a tenant mismatch (FINALSTEP4).

## EF query filters

* Global query filters are applied **only** to `ITenantScoped` entities:
  `Role`, `RolePermission`, `TeacherPlatformMembership`, `Course`, `Unit`, `Lesson`, `Enrollment`,
  `StudentWallet`, `FinancialTransaction`, `TransferRequest`, `StudentCoupon`.
* **Central** entities — `Teacher`, `TeacherPlatform`, `Permission`, `Student`, `StudentFollow` — are
  **not** filtered.

## Application-level membership checks

* `PlatformAccessGuard.RequireMemberAsync` / `RequireOwnerAsync` validate membership/ownership in the
  resolved tenant, throwing `TenantMismatchException` (403).
* This is a backstop beyond the permission policy.

## Central vs tenant-scoped data

* Central: Student, StudentFollow, Teacher, TeacherPlatform, Permission.
* Tenant-scoped (filtered): Role, RolePermission, TeacherPlatformMembership, Course, Unit, Lesson,
  Enrollment, StudentWallet, FinancialTransaction, TransferRequest, StudentCoupon.
* Central: Student, StudentFollow, Teacher, TeacherPlatform, Permission.
* **Why Student/StudentFollow are central:** a student has one identity across platforms and follows
  teachers cross-tenant; they are not owned by any single tenant.
* **Why Course/Unit/Lesson/Enrollment are tenant-scoped:** they are content/academic records owned by a
  specific Teacher Platform. The Student stays central; the Enrollment (the academic relationship) belongs
  to the tenant whose Course it references.
* **Why StudentWallet/FinancialTransaction/TransferRequest are tenant-scoped:** the wallet and its financial
  records belong to the Teacher Platform that operates them (`AGENTS.md` §12 — the Central Platform does not
  own student wallets). This keeps Student A's wallet, transactions, and transfer requests fully isolated
  from Student B and from other tenants.

## How cross-tenant attacks/access are prevented — concrete example

* `Teacher A` JWT → `/{TenantB-PublicId}/{TenantB-Slug}/api/platform/courses`:
  * Route resolves TenantB context.
  * The request is authenticated (Teacher A).
  * **Permission policy** passes if Teacher A's JWT claims a course permission (unlikely for A, but not
    sufficient by itself).
  * **Application guard** (`RequireMemberAsync`) checks whether Teacher A is a member of TenantB — it is
    not → `TenantMismatchException` → **403**.
  * Even if an application bug bypassed the guard, the **EF tenant filter** would prevent any TenantB
    course rows from being returned or from resolving by query.

---

# 8. API Evolution

## Step 4 (foundation)

| Area | Route | Principal | Authz | Purpose |
|------|-------|-----------|-------|---------|
| Teacher registration | `POST /api/central/teachers/register` | anonymous | — | Create a teacher |
| Platform creation | `POST /api/central/platforms` | anonymous | — | Create a platform + owner |
| Platform activation | `POST /api/central/platforms/{publicId}/activate` | anonymous | — | Activate a platform |
| Login | `POST /api/auth/login` | anonymous | — | Issue a Teacher JWT (platform-scoped, requires PublicId) |
| Platform access | `GET /{publicId}/{slug}/api/platform/me` | teacher | `Platform.Access` + membership | Prove slice end-to-end |
| Health | `GET /health` | anonymous | — | Liveness probe (Step 5) |

## Task 1 (platform management)

| Area | Route | Principal | Authz | Purpose |
|------|-------|-----------|-------|---------|
| Profile get/update | `GET`/`PUT /{publicId}/{slug}/api/platform/profile` | teacher | `Platform.Manage` | Read/update platform profile |
| Members list | `GET /{publicId}/{slug}/api/platform/members` | teacher | `Platform.Manage` | List members |
| Add member | `POST /{publicId}/{slug}/api/platform/members` | teacher | `Platform.Membership` (owner) | Add a teacher as member |
| Change role | `PUT/PATCH /{publicId}/{slug}/api/platform/members/{teacherId}` | teacher | `Platform.Membership` (owner) | Change member role |
| Remove member | `DELETE /{publicId}/{slug}/api/platform/members/{teacherId}` | teacher | `Platform.Membership` (owner) | Remove a member |

## Task 2 (student identity & following)

| Area | Route | Principal | Authz |
|------|-------|-----------|-------|
| Student register | `POST /api/student/register` | anonymous | — |
| Student login | `POST /api/student/login` | anonymous | — |
| Student profile | `GET /api/student/me` | student | `[RequirePrincipalType("student")]` |
| Follow | `POST /api/student/follow/{teacherPublicId}` | student | principal_type student |
| Unfollow | `DELETE /api/student/follow/{teacherPublicId}` | student | principal_type student |
| Following list | `GET /api/student/following` | student | principal_type student |
| Is-following | `GET /api/student/following/{teacherPublicId}` | student | principal_type student |

Student login does **not** require a Platform PublicId (central identity). Student JWT carries no
permission claims, so students cannot access teacher-management endpoints.

## Task 3 (course content)

| Area | Route | Principal | Authz |
|------|-------|-----------|-------|
| List courses | `GET /{publicId}/{slug}/api/platform/courses` | teacher | `Course.View` |
| Get course | `GET /{publicId}/{slug}/api/platform/courses/{courseId}` | teacher | `Course.View` |
| Create course | `POST /{publicId}/{slug}/api/platform/courses` | teacher | `Course.Manage` |
| Update course | `PUT /{publicId}/{slug}/api/platform/courses/{courseId}` | teacher | `Course.Manage` |
| Delete course | `DELETE /{publicId}/{slug}/api/platform/courses/{courseId}` | teacher | `Course.Manage` |
| Units | `POST`/`PUT`/`DELETE /courses/{courseId}/units[/{unitId}]` | teacher | `Course.Manage` |
| Lessons | `POST`/`PUT`/`DELETE /courses/{courseId}/units/{unitId}/lessons[/{lessonId}]` | teacher | `Course.Manage` |

All course endpoints additionally require tenant membership via `PlatformAccessGuard.RequireMemberAsync`.

## Task 4 (student enrollment)

**Student-perspective endpoints** (principal `student`, `[RequirePrincipalType("student")]`):

| Area | Route | Purpose |
|------|-------|---------|
| Enroll | `POST /api/student/enroll/{teacherPublicId}/{courseId}` | Enroll the central student in a tenant Course |
| My enrollments | `GET /api/student/enrollments/{teacherPublicId}` | List the student's enrollments in a teacher's platform |
| Cancel | `DELETE /api/student/enrollments/{teacherPublicId}/{courseId}` | Cancel the student's enrollment |

**Teacher-perspective endpoint** (principal `teacher`, `Enrollment.View` + membership):

| Area | Route | Purpose |
|------|-------|---------|
| Course enrollments | `GET /{publicId}/{slug}/api/platform/courses/{courseId}/enrollments` | List a course's enrollments (read-only) |

## Task 5 (student wallet & course purchase)

**Student-perspective endpoints** (principal `student`, `[RequirePrincipalType("student")]`):

| Area | Route | Purpose |
|------|-------|---------|
| Submit transfer | `POST /api/student/wallet/{publicId}/transfer` | Request a wallet-credit Transfer Request |
| My wallet | `GET /api/student/wallet/{publicId}` | Read the student's wallet balance + transaction history (empty wallet → 204) |
| Purchase course | `POST /api/student/purchase/{publicId}/{courseId}` | Atomically purchase a Paid course (debit + enrollment + transaction) |

**Teacher-perspective endpoints** (principal `teacher`, `Wallet.Manage` + membership):

| Area | Route | Purpose |
|------|-------|---------|
| List transfer requests | `GET /{publicId}/{slug}/api/platform/wallet/transfers` | List pending/all transfer requests for the platform |
| Approve transfer | `POST .../wallet/transfers/{requestId}/approve` | Approve and credit the student wallet (idempotent, double-approve → 422) |
| Reject transfer | `POST .../wallet/transfers/{requestId}/reject` | Reject without crediting |

A cross-tenant transfer review returns **404** (the transfer is not resolvable in the acting tenant).

## Task 6 (student coupons)

**Teacher-perspective endpoints** (principal `teacher`, `Coupon.Manage` + membership):

| Area | Route | Purpose |
|------|-------|---------|
| Create coupon | `POST /{publicId}/{slug}/api/platform/coupons` | Create a single-use, Course-specific student coupon |
| List coupons | `GET /{publicId}/{slug}/api/platform/coupons` | List the platform's coupons |
| Get coupon | `GET /{publicId}/{slug}/api/platform/coupons/{couponId}` | Get one coupon (incl. consumption/status) |
| Revoke coupon | `DELETE /{publicId}/{slug}/api/platform/coupons/{couponId}` | Revoke (expire) an active coupon |

**Student-perspective** — the purchase endpoint now accepts an optional body `{ couponCode }` to apply a coupon:

| Area | Route | Purpose |
|------|-------|---------|
| Purchase with coupon | `POST /api/student/purchase/{publicId}/{courseId}` (body `{ couponCode }`) | Atomically purchase a Paid course applying an optional single-use coupon |

---

# 9. Database Evolution

## Initial schema (InitialCreate migration `20260901233129`)

* `teachers` (`Email` UNIQUE)
* `teacher_platforms` (`PublicId` UNIQUE; `Slug` non-unique index)
* `permissions`
* `roles`
* `role_permissions` (join)
* `teacher_platform_memberships` (join with role/ownership)

Tenant-scoped entities: `roles`, `role_permissions`, `teacher_platform_memberships` (under tenant query filter).
Global entities: `teachers`, `teacher_platforms`, `permissions` (not filtered).

## Task 2 (AddStudentFollow migration `20260902231809`)

* `students` (central; `Email` UNIQUE via `ux_students_email`)
* `student_follows` (central; `ux_follows_student_teacher` unique `(StudentId, TeacherId)`; index on teacher; FK student→teacher RESTRICT)
* Both central tables carry **no** `tenant_id` and are **not** under the tenant filter.

## Task 3 (AddCourseContent migration `20260903014302`)

* `courses`, `units`, `lessons` (all tenant-scoped with `TenantId` FK)
* FKs: Unit→Course, Lesson→Unit (cascade delete — approved hard-delete decision at this stage)
* Non-unique lookup indexes on position (positions are enforced by the domain aggregate, not the DB)
* `Course.Title` NOT unique (duplicate titles allowed)
* No Course PublicId / slug / index

## Task 4 (AddEnrollment migration `20260903171426`)

* `enrollments` (tenant-scoped with `TenantId` FK)
* `ux_enrollments_student_course` unique `(TenantId, StudentId, CourseId)` — prevents duplicate enrollment
  at the DB level.
* `Enrollment → Course` FK uses **Restrict** (no cascade delete).
* `EnrollmentStatus` persisted via a value converter.

## Task 5 (wallet & financial migrations)

* `20260903184016_AddWalletAndFinancialTransactions`:
  * `student_wallets` (tenant-scoped, unique `(StudentId, TenantId)` — one wallet per student per tenant).
  * `financial_transactions` (tenant-scoped, immutable audit records with type/direction/status/amount).
  * `transfer_requests` (tenant-scoped, wallet-credit flow states).
* `20260903192311_ReworkEnrollmentUniqueConstraintForReEnrollment`:
  * Drops the full unique `ux_enrollments_student_course` index and recreates it as a **partial unique
    index** (`WHERE status = Active`) so a student may hold only one **Active** enrollment per course while
    terminal (cancelled) history may coexist — enabling re-enrollment after cancellation.

## Task 6 (student coupon migrations)

* `20260903221349_AddStudentCoupons`:
  * `student_coupons` (tenant-scoped with `TenantId` FK).
  * Unique `(TenantId, Code)` — one coupon code per tenant.
  * `AssignedToStudentId` (FK to central students) and `CreatedByTeacherId` (FK to teachers).
  * `DiscountType`, `DiscountValue`, `ExpiresAt`, `Status`, `ConsumedAt`, `ConsumedInTransactionId`.
  * Lookup indexes for tenant and student.
* `20260904005926_AddCourseIdToStudentCoupons`:
  * Adds the required `CourseId` FK to `student_coupons` (per the approved Course-applicability correction:
    every `StudentCoupon` is tied to exactly one specific Course).
* The single-use rule is enforced both by application logic (`Consume` state transition) and by the
  `SELECT ... FOR UPDATE` row lock inside the explicit purchase transaction.

## Important database design decisions

* **PublicId globally unique; Slug non-unique** (approved rule — slug is a URL component, not identity).
* **Query filters only on tenant-scoped entities** — central identity data is never hidden from central operations.
* **Duplicate follow prevented at the DB level** (`ux_follows_student_teacher`), not only in application code.
* **Duplicate enrollment prevented at the DB level** (`ux_enrollments_student_course`), not only in
  application code.
* **Enrollment→Course Restrict FK** — cancelled/completed enrollments are historical academic records and
  must survive course deletion; Unit/Lesson cascade behavior is unaffected.
* **Position uniqueness deferred to the domain aggregate** (approved deviation) rather than DB unique
  constraint (see Section 10).
* **Financial/audit records** (wallets, purchases, transfer requests implemented in Task 5; refunds**
  **and coupons reserved) are treated as historical and are not casually deleted.
* **Wallet uniqueness** — `(StudentId, TenantId)` is unique per tenant (one wallet per student per
  Teacher Platform), reinforcing the tenant-scoped wallet model.
* **Enrollment uniqueness (partial)** — only one **Active** enrollment per `(student, course)` is allowed;
  cancelled history may coexist (re-enrollment after terminal cancellation).

---

# 10. Important Technical Problems and Fixes

## JWT KeyId issue

* **Symptom:** Signature validation failed or diverged between token issuance and validation.
* **Root cause:** IdentityModel 8+/JWT bearer requires a matching `kid` header for symmetric signing key
  validation.
* **Fix:** A stable configured `KeyId` (`JwtOptions.KeyId`) used by both issuance and validation.
* **Why:** Ensures the same key/header is used on both sides.
* **Architecture change:** No — configuration only.

## Cross-tenant JWT binding correction (and reversal)

* **Symptom (SECURITYFIX):** Tenant isolation seemed to depend on per-service membership checks.
* **Root cause:** `TenantRouteMiddleware` did not enforce JWT-tenant matching.
* **Fix (SECURITYFIX):** A middleware-level binding rejected authenticated requests with a mismatched
  JWT tenant.
* **Second problem (FINALSTEP4):** That binding was too broad — it blocked authenticated cross-tenant
  **public** browsing, which the product requires.
* **Final fix (FINALSTEP4):** Removed the overly broad middleware rejection; kept tenant access
  enforcement in permission policies + membership guards (defense in depth).
* **Architecture change:** Yes — the security model was refined to allow cross-tenant public browsing
  while protecting tenant-management endpoints. This is a documented, deliberate decision.

## Npgsql/PostgreSQL & EF Core version alignment

* **Symptom:** MSB3277 assembly conflicts when consumers combined Npgsql's transitive EF with the
  explicitly referenced EF version.
* **Root cause:** Version divergence across the dependency graph.
* **Fix:** Pinned EF Core packages to 10.0.11 across the graph (explicit public references in
  Infrastructure), aligned with `Microsoft.EntityFrameworkCore.Design` 10.0.11.
* **Architecture change:** No.

## EF Core value-converter name shadowing

* **Symptom/root cause:** Bare value-converter field names shadowed type aliases during compilation.
* **Fix:** Expose named static fields (`EmailConverter`, `PublicIdConverter`, `SlugConverter`).
* **Architecture change:** No.

## Testcontainers port issue

* **Symptom:** On this Docker Desktop host, `GetConnectionString()`/`GetMappedPublicPort` returned the
  container's internal port (5432), so `WebApplicationFactory` reached an unbound port.
* **Fix:** Pin an explicit host port (`WithPortBinding(5433, 5432)`) and drive the connection string
  through `builder.UseSetting(...)`.
* **Architecture change:** No — test-harness fix.

## EF tracking of new child entities (Task 3)

* **Symptom:** Adding a Unit/Lesson raised `DbUpdateConcurrencyException` (409).
* **Root cause:** EF relationship fixup marked the brand-new child as `Modified` → an `UPDATE` against
  a nonexistent row.
* **Fix:** Add explicit `AddUnit`/`AddLesson` to the repository and call them before `SaveChanges`.
* **Architecture change:** No.

## Ordering/reordering vs unique index conflict (Task 3)

* **Symptom:** Atomic reordering under an immediate-unique `(parent, position)` index caused an EF
  "circular dependency" topological-ordering error.
* **Root cause:** EF Core's change-tracker cannot order multi-row reorders within a single save when
  each intermediate state violates an immediate unique constraint.
* **Fix:** Approved domain-only uniqueness (`Tasks/Approved1.md`): omit DB unique position indexes and
  enforce unique/contiguous ordering via the Course/Unit aggregates (single writers), keeping
  reordering atomic through `IUnitOfWork`.
* **Why this fix:** No deferrable constraints, hand-managed migration SQL, or EF snapshot hacks were
  permitted; the aggregate is the correct single writer for ordering state.
* **Architecture change:** No — an approved deviation recorded in docs.

## Include-collection ordering (Task 3)

* **Symptom:** A move-ordering integration test was flaky inside the full suite.
* **Root cause:** Database row order for the included Units/Lessons was undefined.
* **Fix:** The repository explicitly orders Units by Position (and Lessons by Position) in `GetByIdAsync`.
* **Architecture change:** No.

## Enrollment LINQ translation (Task 4)

* **Symptom:** `EnrollmentRepository` quyery with ordering applied before a `Status.ToString()`
  projection threw a LINQ translation error.
* **Root cause:** Certain projections (enum `ToString`) cannot be translated to SQL alongside ordering in a
  single query.
* **Fix:** Two-step materialization — order the rows in SQL, then project `Status.ToString()` in memory.
* **Architecture change:** No.

## Contract `[Required]` causing API 4xx/5xx (Task 5)

* **Symptom:** `POST .../wallet/{publicId}/transfer` returned model-state errors (400/500) instead of the
  application response.
* **Root cause:** `[Required]` attributes on the `SubmitTransferRequest` DTO let MVC validation reject the
  request before the application service (which already validates the same rules) ran.
* **Fix:** Removed the redundant `[Required]` attributes; validation is owned entirely by the application
  service.
* **Architecture change:** No — contract simplification.

## Cross-tenant transfer review status (Task 5)

* **Symptom:** `Owner_CannotReviewAnotherTenantsTransfer` expected 403 but the API returned 404.
* **Root cause:** For the acting tenant, another tenant's `TransferRequest` id is not resolvable, so the
  handler resolves it as NotFound.
* **Fix (decision):** A cross-tenant transfer review returns **404 (NotFound)** — consistent with an
  un-resolvable resource in the acting tenant. The integration test was updated to assert 404.
* **Architecture change:** No — status-code semantics.

## Empty student wallet response (Task 5)

* **Symptom:** `Student_CannotAccessAnotherStudentsWallet` expected 200 with an empty body but got 204.
* **Root cause:** A not-yet-created (empty, lazy) wallet caused `Ok(null)`, which MVC serializes as
  **204 No Content** with no body.
* **Fix:** The integration test now asserts 204 No Content for a student whose wallet has no history.
* **Architecture change:** No — response semantics.

## Re-enrollment blocked by full unique index (Task 5)

* **Symptom:** `Student_RepurchasesAfterTerminalCancellation_Permitted` failed because a cancelled
  enrollment still owned the `(student, course)` unique pair, so re-purchase returned 422 "already
  enrolled".
* **Root cause:** The Task 4 `ux_enrollments_student_course` index was a **full** unique index on
  `(student_id, course_id)`.
* **Fix (approved decision):** Converted it to a **partial unique index** (`WHERE status = Active`),
  `GetActiveAsync` in the enrollment repository, and duplicate checks in Enroll/Purchase switched to
  rejecting only a duplicate **Active** enrollment. Re-enrollment after a terminal cancellation is now
  permitted while prior history is preserved.
* **Architecture change:** Yes, but a small, approved data-constraint change surfaced via AGENTS.md §30.

## Coupon double-consumption race (Task 6)

* **Symptom (final review):** Two concurrent purchases could both read the same coupon before either consumed it,
  because the `SELECT ... FOR UPDATE` lock might be released before the enclosing `SaveChanges`/commit.
* **Root cause:** The coupon FOR UPDATE read was not guaranteed to run inside the same database transaction that
  stays open until commit.
* **Fix:** Added the smallest appropriate transaction abstraction — `IUnitOfWork.ExecuteInTransactionAsync(...)` —
  implemented in `EfUnitOfWork` by opening an explicit `IDbContextTransaction` through the EF execution strategy
  (preserving retry behavior). `PurchaseCourseService` runs the entire coupon purchase (FOR UPDATE read → validate →
  debit → consume → enroll → financial records → SaveChanges) inside this one transaction, so the row lock is held
  until commit and a concurrent attempt observes the consumed state and fails cleanly.
* **Architecture change:** Minor and consistent with the existing architecture — no new persistence layer, no CQRS,
  no event bus, no parallel purchasing infrastructure.

## Concurrency-test tenant-scope setup (Task 6)

* **Symptom:** The first version of the concurrency integration test set a tenant context on each service before
  calling `PurchaseAsync`, which failed with `TenantMismatchException` ("A central operation cannot run under a
  teacher tenant context").
* **Root cause:** `PurchaseCourseService.PurchaseAsync` is a **central** operation that refuses to run under an
  active teacher tenant scope; it switches tenant internally via `ITenantContext.TrySetTenant`.
* **Fix:** Each service-under-test now starts with a **null (central)** tenant scope — exactly like the real API
  request — and lets `PurchaseCourseService` switch/restore the tenant itself.
* **Architecture change:** None — corrected the test to match the architecture.

---

# 11. Important Decisions / ADR-style Summary

| Decision | Chosen approach | Why | Alternatives considered | Status |
|----------|-----------------|-----|--------------------------|--------|
| Platform PublicId + Slug | Unique non-sequential `PublicId` + non-unique `Slug`; route validates both | Stable public identity; slug is a URL component, not identity | Slug-as-identity | Approved |
| No Student PublicId | Student uses internal Guid as `sub`; no public URL | No approved public student URL requirement | Student PublicId | Approved |
| Student following via Platform PublicId → Owner Teacher | `{teacherPublicId}` is a platform PublicId; follow resolves to the Owner teacher | Follows teachers, not platforms | Direct teacher PublicId | Approved |
| Principal type | Add `principal_type` claim; separate teacher/student handlers | Prevent student/teacher confusion; minimal extension | New auth mechanism | Approved |
| Tenant isolation model | TenantContext + EF filters + application membership guards | Defense in depth; cannot rely on one mechanism | Middleware-only binding (rejected) | Approved |
| Dynamic permissions | Role + Permission; `Platform.Access`/`Manage`/`Membership`/`Course.View`/`Course.Manage` | Avoid specialized roles | Per-feature roles | Approved |
| Course Draft/Published | Minimal two-state lifecycle | Distinguish in-preparation vs published | Complex workflow | Approved |
| No Course PublicId/Slug | Course managed by internal Guid; no public course URL | No approved public course URL requirement | Course slug | Approved |
| Duplicate course titles allowed | No unique constraint on Title | Title is descriptive, not identity | Unique titles | Approved |
| Course content hard delete before Enrollment | Hard delete + cascade (no enrollment yet) | No student references yet; revisit later | Soft delete | Approved |
| Domain-only ordering uniqueness | No DB unique position index; aggregate is single writer | EF topological conflict with atomic reorder | Deferrable constraints (rejected) | Approved (deviation) |
| Cross-tenant public browsing | TenantRouteMiddleware resolves but does not reject on JWT mismatch | Product requires browsing another tenant's public content | Middleware binding (rejected) | Approved |
| Enrollment entity + lifecycle | Tenant-scoped `Enrollment` with `Active/Cancelled` status | Core academic relationship, status-managed; only Active can be cancelled, Cancelled is terminal | Freeform no-status record | Approved |
| Enrollment.View permission | Read-only course-enrollment access as a dynamic permission | Follows Course.View precedent; permissions, not roles | Per-feature role | Approved |
| Enrollment ≠ Following | Separate; enrolling does not auto-follow, following does not enroll | Follow is a central interest signal; Enrollment is the academic relationship | Merge the two | Approved |
| Duplicate-enrollment & delete semantics | `ux_enrollments_student_course` unique index + `Enrollment→Course` Restrict FK | DB-level single-enrollment guarantee and preserved academic history | App-level check + cascade | Approved |
| Course pricing | Explicit `CoursePricingType` (Free/Paid) + EGP `Price` via `SetPricing` | Free vs Paid is an explicit state, never inferred from `Price` | Infer from `Price == null/0` (rejected) | Approved |
| Student wallet ownership | Tenant-scoped `StudentWallet`, balance derived from `FinancialTransaction` history | Wallet belongs to the Teacher Platform; financial records not dependent on a mutable balance | Central-owned wallet (rejected) | Approved |
| Wallet credit flow | `TransferRequest` submit → approve/reject; approve idempotent (double-approve 422) | Auditable, controlled wallet funding | Direct balance mutation | Approved |
| Atomic course purchase | `PurchaseCourseService` validates+debts+enrolls+records transaction in one UoW | Prevent partial/negative/double-spend; duplicate active purchase 422 | Non-atomic multi-step | Approved |
| Re-enrollment after cancellation | Partial unique `(student, course)` index (`WHERE status = Active`); duplicate-check on Active only | Preserve history yet permit re-enrollment after terminal cancellation | Full unique index (rejected — blocked re-enrollment) | Approved |
| Wallet financial types | `Refund` + `CouponCredit` `TransactionType` reserved only (not implemented) | Forward-compatible, no invented flows | Implement refunds/coupons now (rejected) | Approved |
| Free course enrollment | Free course → direct-enroll (no wallet/purchase); purchase endpoint rejects Free | Keep Free/Paid flows distinct; do not break Free enrollment | Route all enrollments through purchase (rejected) | Approved |
| Student coupon scope | Tenant-scoped, single-use `StudentCoupon` assigned to one student; Percentage 1–100% + Fixed capped | Personal, non-transferable, expirable, single-use per AGENTS.md §12 | Global/transferable coupons (rejected) | Approved |
| Coupon Course applicability | Every `StudentCoupon` is tied to exactly one specific Course (`CourseId` required) | Final review approved per-course coupon; supersedes "all Paid Courses / no CourseId" | All-Paid-Courses coupon (rejected) | Approved |
| CouponCredit semantics | Informational/audit only; never credits wallet; 100% discount yields no zero-amount Purchase tx | Records the covered value without altering balance | Credit the wallet (rejected) | Approved |
| Coupon purchase atomicity | Whole coupon purchase in one explicit transaction; `SELECT ... FOR UPDATE` held until COMMIT | Prevent concurrent double consumption; AGENTS.md §15–§16 | App-level check only (rejected) | Approved |
| Coupon.Manage permission | Single dynamic permission for teacher coupon create/list/get/revoke | Avoid specialized roles | Per-op roles (rejected) | Approved |
| Coupon refund boundary | Refunds deferred to Task 7 | Task 6 does not handle refunds | Implement refunds now (rejected) | Approved |

---

# 12. Testing Strategy Evolution

## Layered tests

* **Domain tests:** value objects, aggregators, invariants, public ID/slug, status transitions,
  course/unit/lesson ordering.
* **Application service tests:** use cases with fake repositories/ports; edge cases, business rules,
  tenant mismatch.
* **Authorization tests:** claim construction (JWT), permission policy handling, principal-type
  separation.
* **Integration tests:** full stack against a real PostgreSQL 16 Testcontainer via
  `WebApplicationFactory` + shared fixture; tenant isolation, auth, routing, and feature scenarios.

## Test totals by phase (reliably recorded)

| Phase | Unit | Integration |
|-------|------|-------------|
| Step 3 | 62+ (confirmed green) | — |
| Step 4 | — | 14 |
| Step 6 | 135 | 14 |
| Step 8 | 135 | 14 |
| Task 1 | 174 | 26 |
| Task 2 | 221 | 39 |
| Task 3 | 281 | 51 |
| Task 4 | 315 | 64 |
| Task 5 | 387 | 77 |
| Task 6 | 451 | 90 |

## Notable integration coverage

* Tenant A cannot access Tenant B data.
* PublicId/404, wrong-slug/301, anonymous/401, missing-permission/403.
* Duplicate email/409; duplicate follow/422; blank-title/400; business-rule/422.
* Cross-tenant and principal-type separation.
* Full slice: register → create → activate → login → access.
* Task 4: 28 application-service tests (enroll/list/cancel/course-list) + 13 integration scenarios across
  `EnrollmentTests.cs` (enroll, list, cancel, duplicate-enrollment 422, teacher read, tenant isolation).
* Task 5: 7 new application-service suites (purchase, submit/review/list transfer, list wallet) + 13
  integration scenarios in `WalletAndPurchaseTests.cs` (submit/approve flow, double-approve 422, reject,
  fund+purchase+enrollment+debit, insufficient balance 422, draft-course purchase 422, free-through-purchase
  422 + free direct-enroll, duplicate active purchase 422, repurchase-after-cancellation permitted,
  cross-tenant review 404, anonymous 401, assistant-without-`Wallet.Manage` 403, cross-student wallet 204).
* Task 6: coupon management (create/list/get/revoke, duplicate code 422, free-course coupon 422), purchase with a
  Partial/Fixed/100% coupon (correct final debit + CouponCredit, consumption, expiry/wrong-course/wrong-student/
  unknown/consumed-coupon failures with no side effects), cross-tenant coupon isolation, anonymous/permission
  authorization, and the **real-concurrency test** (`ConcurrentCouponPurchaseTests`) firing two genuinely
  concurrent purchases against the same tenant/student/course/coupon on separate connections — exactly one
  succeeds, one enrollment, one consumption, one wallet debit, no duplicate financial transactions.

---

# 13. Git / Commit History

## Logical commit groups

* **scaffold** (`6c7533b`) — .NET 10 solution, projects, Docker PostgreSQL setup.
* **domain** (`41f46dd`) — Step 1 domain foundation.
* **infrastructure** (`2b63650`) — Step 2 DbContext/tenant/migration.
* **application** (`963c541`) — Step 3 use cases, ports, results, unit tests.
* **api** (`99ab681`) — Step 4 API, JWT, permissions, tenant routing.
* **security** (`8b0f9ab` SECURITYFIX, `34781f9` FINALSTEP4) — tenant binding correction + reversal for public browsing.
* **docker** (`f57b2fa`) — Step 5 hardened runtime.
* **checkpoint** (`b4079f1`) — development break record.
* **test** (`d338e6a`) — Step 6 claim/permission/state tests.
* **verification** (`ce17356`) — Step 7 containerized smoke test.
* **git** (`fd10cce`) — Step 8 repository strategy finalization.
* **Task 1** — `891d1e9` domain, `b240283` infra, `de8ec00` application, `da1d74c` api, `ac3e921` test, `c1ff6bf` docs.
* **Task 2** — `3c36dff` domain, `10c2a81` infra, `8780be2` application, `509962e` api, `9bd8163` test, `fa6d7e7` docs.
* **Task 3** — `c79cbc9` domain, `9f59091` infra, `8bdd2fe` application, `796ce20` api, `a5e39cc` test, `9d876e8` docs.
* **Task 4** — `71d215b` domain, `bc6b389` infra, `f5f2940` application, `b747e7b` api, `d42ef46` test, and
  the docs commit for this update (`docs: ...`).
* **Task 5** — `da6b603` domain (wallet/transaction/transfer/pricing), `410c8ce` infra (persistence +
  `AddWalletAndFinancialTransactions`), `ddf27d4` application (repos + DTOs), `eba4d42` application
  (use cases), `33c8bef` api (endpoints), `b0dd333` application (active-enrollment lookup for
  re-enrollment), `85c95e1` infra (partial unique index migration), `37d15af` api (pricing + contract
  cleanup), `658d66d` test (unit + integration coverage).

## Current repository state

* Branch: `main`, ahead of `origin/main` by 42 commits.
* Working tree: clean except the in-progress Task 5 documentation update on
  `IMPLEMENTATION_PLAN.md` / `PROJECT_IMPLEMENTATION_HISTORY.md` and the untracked baseline planning files
  (`PROJECT_DOCUMENTATION/`, `SETUPDOCUMENT.md`, `Tasks/3-9-1.md`, `Tasks/3-9.md`, `Tasks/Approved2.md`,
  `Tasks/TASK5.md`, `Tasks/TASK5_PLAN.md`, `Tasks/TASKREVIEW.md`).
* **Nothing has been pushed to `origin`** (documented throughout).
* Current checkpoint: after completed Task 5 (Student Wallet & Course Purchase).

---

# 14. Current Architecture Snapshot (AS IT EXISTS NOW)

## Central Platform

* Teacher registration, platform creation, platform activation.
* Dynamic permission catalog and role model; permission seeding.
* Teacher login (platform-scoped, requires PublicId).

## Teacher Platform

* Tenant-scoped profile and membership management (owner-only mutations; last-owner protection).
* Tenant-scoped course content: Course → Unit → Lesson with explicit ordering.
* Tenant-scoped Student Wallets, Financial Transactions, and Transfer Requests (wallet credit + audit).
* Membership-based tenant access via `PlatformAccessGuard`.

## Student

* Central identity (registration, login without PublicId, profile).
* Follow/unfollow/list/is-following teachers (central, DB-unique).
* Enroll in / list / cancel Course Enrollments (tenant-scoped academic relationship, central identity).
* Wallet: submit transfer requests, read wallet + transaction history, and atomically purchase Paid Courses.
* Following does not grant platform-management access; Enrollment ≠ Following.

## Authentication

* JWT bearer; `principal_type` distinguishes teacher vs student.
* Teacher tokens carry platform/role/permission claims; student tokens carry no such claims.

## Courses

* Tenant-scoped `Course` (Draft/Published, explicit Free/Paid pricing) → `Unit` (Position) → `Lesson` (Position).
* No public course URLs; internal Guid identity; duplicate titles allowed; hard delete/cascade.
* Tenant-scoped `Enrollment` (Active/Cancelled) links a central Student to a Course
  (partial unique `ux_enrollments_student_course` allowing one Active enrollment; `Enrollment → Course` Restrict FK).

## Database

* Major entities: `teachers`, `teacher_platforms`, `permissions`, `roles`, `role_permissions`,
  `teacher_platform_memberships`, `students`, `student_follows`, `courses`, `units`, `lessons`,
  `enrollments`, `student_wallets`, `financial_transactions`, `transfer_requests`.
* Tenant-scoped (filtered): roles, role_permissions, memberships, courses, units, lessons, enrollments,
  student_wallets, financial_transactions, transfer_requests.
* Central (not filtered): teachers, platforms, permissions, students, student_follows.

## API

* Central: `/api/central/teachers/register`, `/api/central/platforms`, `/api/central/platforms/{publicId}/activate`.
* Auth: `/api/auth/login`.
* Teacher Platform management: `/{publicId}/{slug}/api/platform/{profile,members,...}`.
* Teacher Platform course content: `/{publicId}/{slug}/api/platform/courses...`.
* Teacher Platform enrollments: `/{publicId}/{slug}/api/platform/courses/{courseId}/enrollments` (`Enrollment.View`).
* Teacher Platform wallet: `/{publicId}/{slug}/api/platform/wallet/transfers...` (list/approve/reject, `Wallet.Manage`).
* Student: `/api/student/{register,login,me,follow...,following...}` + `/api/student/enroll...` (enroll/list/cancel)
  + `/api/student/wallet/{publicId}` (transfer + wallet) + `/api/student/purchase/{publicId}/{courseId}`.
* Health: `/health`.

## Security

* TenantRouteMiddleware resolves the tenant (404/301/continue) without global JWT-tenant rejection.
* Permission policies + application membership guards enforce tenant access (defense in depth).
* EF tenant query filters guard tenant-scoped data at the data layer (including wallets/finance).
* Financial operations (purchase, wallet credit) are transactional and idempotency-protected.

## Tests

* Unit 387/387; integration 77/77; build 0 warnings / 0 errors (Task 5 baseline).

---

# 15. Current Roadmap / Where We Stopped

## Completed

* Steps 0–8 (foundation + vertical slice + Dockerization + verification + git).
* Task 1 — Teacher Platform Management.
* Task 2 — Central Student Identity & Following.
* Task 3 — Teacher Platform Course Content.
* Task 4 — Student Enrollment in Teacher Courses.
* Task 5 — Student Wallet & Course Purchase.

## Future / planned (not yet implemented)

* Task 6 and beyond — see `IMPLEMENTATION_PLAN.md` and approved `Tasks/` documents. Candidate roadmap
  items (carried in the roadmap/plan) include completion/progress tracking, coupons, refunds (types
  reserved in Task 5 but not implemented), and potential future Enrollment lifecycle states beyond
  Active/Cancelled.

---

# 16. How to Continue the Project (Continuation Protocol)

A future implementation session should:

1. Read `AGENTS.md`.
2. Read `IMPLEMENTATION_PLAN.md`.
3. Read `PROJECT_IMPLEMENTATION_HISTORY.md`.
4. Read the relevant Task document (e.g. `Tasks/TASK6.md`/the next approved Task document).
5. Confirm the current checkpoint (currently: after completed Task 5 — Student Wallet & Course Purchase).
6. Review any pending roadmap decisions for the next task.
7. Obtain explicit human approval before implementing.
8. Implement only the approved task.
9. Test and document it.
10. Update this history document.

This protocol prevents losing project context between sessions and preserves the reasoning behind the
architecture.
