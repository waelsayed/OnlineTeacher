# Task 3 — Implementation Authorization

## Teacher Platform Course Content: Courses → Units → Lessons

Task 3 planning has been reviewed and APPROVED.

You may now implement Task 3 according to the approved decisions below.

## Current approved state

Steps 0–8 are complete and approved.

Task 1 — Teacher Platform Management:

* Complete and approved.
* Latest Task 1 commit: `c1ff6bf`

Task 2 — Central Student Identity & Following:

* Complete and approved.
* Latest Task 2 commits:

  * `3c36dff`
  * `10c2a81`
  * `8780be2`
  * `509962e`
  * `9bd8163`
  * `fa6d7e7`

Current baseline:

* Unit tests: 221/221
* Integration tests: 39/39
* Build with `--warnaserror`: 0 warnings / 0 errors
* Working tree is clean.
* Repository is ahead of origin.
* DO NOT push anything.

---

# Approved Task 3 scope

Implement the Teacher Platform tenant-scoped educational content hierarchy:

**Course → Unit → Lesson**

This is a Teacher Platform capability.

It is the foundational content structure required by future Enrollment and other educational features.

## Explicitly OUT OF SCOPE

Do NOT implement:

* Student enrollment
* Course purchasing
* Payments
* Wallet
* Course pricing
* Coupons
* Entitlements/access control
* Student course access
* Student progress
* Student dashboards
* Exams
* Homework
* Revision
* Reviews/ratings
* Notifications
* Analytics
* Actual video/file/media upload or playback
* Public/anonymous course browsing
* Student-visible course pages
* New Teacher-level PublicId
* Course PublicId
* Course public URLs
* Course slug routing

Do not pull future functionality into this task.

---

# Final business decisions

## 1. Course lifecycle

Course has a minimal status:

* Draft
* Published

Use a simple enum/value representation consistent with the existing domain conventions.

Do NOT build a complex publishing workflow.

The purpose is simply to distinguish content that is still being prepared from content that is published.

Future Enrollment may use Published as a prerequisite, but Enrollment is NOT part of Task 3.

If a status transition requires validation, keep the rule minimal and explicit.

---

## 2. Course identity / slug

IMPORTANT:

**Do NOT add a Course Slug.**

Course management routes use the internal `Guid courseId`.

Do NOT introduce:

* Course PublicId
* Course slug
* public course URL
* new public identity convention

There is currently no approved requirement for public course URLs.

If public course URLs are needed later, they will be designed as a separate decision/task.

---

## 3. Course title uniqueness

Duplicate Course titles within the same Teacher Platform are allowed.

Do NOT create a unique constraint on Course.Title.

Title is descriptive, not identity.

---

## 4. Deletion

For Task 3:

* Course deletion is allowed.
* Unit deletion is allowed.
* Lesson deletion is allowed.
* Hard delete is acceptable.
* Course deletion cascades to its Units and Lessons.
* Unit deletion cascades to its Lessons.

There is currently no Enrollment/student data referencing these objects, so do not introduce soft-delete/archive complexity.

Document that future student/enrollment references may require revisiting deletion semantics.

---

## 5. Ordering

Units and Lessons have explicit integer ordering/position.

Use:

* Unit: `Position`
* Lesson: `Position`

Enforce uniqueness:

* `(CourseId, Position)`
* `(UnitId, Position)`

Do not derive ordering from creation timestamps.

When changing a position, handle conflicts according to a clear deterministic rule. Do not allow duplicate positions.

If implementing automatic position shifting, keep it simple and transactional; do not introduce a general ordering framework.

---

## 6. Parent ownership / tenant isolation

This is mandatory.

For every child operation:

* Resolve the parent inside the current tenant.
* Verify that the parent belongs to the current tenant.
* Never trust a client-supplied `courseId`, `unitId`, or `tenantId`.
* TenantId must come from the resolved TenantContext.
* Cross-tenant access must fail safely.

Examples:

Adding a Unit:
`courseId` must resolve to a Course belonging to the current tenant.

Adding a Lesson:
both the Course and Unit relationship must resolve consistently within the current tenant.

Do not rely solely on controller route values.

Existing EF tenant query filters remain defense-in-depth and must not be replaced by application checks.

---

# Domain model

Implement:

### Course

Tenant-scoped:

* Guid Id
* Guid TenantId
* Title
* Summary
* Status
* ordered Units
* audit fields

### Unit

Tenant-scoped:

* Guid Id
* Guid TenantId
* Guid CourseId
* Title
* Position
* ordered Lessons
* audit fields

### Lesson

Tenant-scoped:

* Guid Id
* Guid TenantId
* Guid UnitId
* Title
* Position
* audit fields

Follow existing domain conventions.

Do not expose persistence entities directly through the API.

---

# Permissions

Add the minimum required platform permissions:

* `Course.View`
* `Course.Manage`

Do not create new roles.

Do not modify the existing role architecture.

Use the existing dynamic role + permission model.

`Course.View` is sufficient for course structure reads.

`Course.Manage` is required for create/update/delete operations.

---

# API

Use tenant-scoped routes consistent with the existing platform API architecture:

`/{publicId}/{slug}/api/platform/courses`

All endpoints are Teacher Platform operations.

No anonymous student endpoints in Task 3.

### Course

POST:
`/courses`

GET:
`/courses`

GET:
`/courses/{courseId}`

PUT:
`/courses/{courseId}`

DELETE:
`/courses/{courseId}`

### Unit

POST:
`/courses/{courseId}/units`

PUT:
`/courses/{courseId}/units/{unitId}`

DELETE:
`/courses/{courseId}/units/{unitId}`

### Lesson

POST:
`/courses/{courseId}/units/{unitId}/lessons`

PUT:
`/courses/{courseId}/units/{unitId}/lessons/{lessonId}`

DELETE:
`/courses/{courseId}/units/{unitId}/lessons/{lessonId}`

Use:

* `[Authorize]`
* existing `RequirePermission`
* existing tenant resolution
* existing `PlatformAccessGuard`
* existing ProblemDetails/error mapping

Do not create a new authorization mechanism.

---

# API behavior

Follow existing project conventions for:

* 401 unauthenticated
* 403 unauthorized / missing permission / not a tenant member
* 404 resource not found
* 400 invalid request
* 422 business-rule violation
* existing conflict/error conventions where applicable

Do not invent arbitrary status-code conventions.

Responses must use DTOs.

Do not expose:

* password data
* internal security information
* persistence implementation details
* unnecessary tenant internals

---

# Application layer

Follow the existing one-service-per-use-case architecture.

Implement only the services actually required for this task, such as:

* CreateCourseService
* UpdateCourseService
* GetCourseService
* ListCoursesService
* DeleteCourseService
* AddUnitService
* UpdateUnitService
* RemoveUnitService
* AddLessonService
* UpdateLessonService
* RemoveLessonService

Use the existing UnitOfWork.

Repositories should remain purpose-specific.

Do NOT introduce:

* Generic repositories
* CQRS
* MediatR
* Event bus
* domain event infrastructure
* unnecessary abstractions

Keep the implementation pragmatic.

---

# Persistence

Add EF configurations and repositories for the new entities.

All three entities must be tenant scoped and included in the existing tenant query-filter mechanism.

Required database relationships:

* Unit → Course
* Lesson → Unit

Required uniqueness:

* `(CourseId, Position)`
* `(UnitId, Position)`

Required indexes for practical lookup.

Course title must NOT be unique.

No Course slug index.

No Course PublicId.

Use cascade delete behavior consistent with the approved hard-delete decision.

Create the required EF migration.

Do not modify unrelated database structures.

---

# Tests

Preserve all existing tests.

Add comprehensive coverage for:

### Domain

* valid Course
* invalid Course title
* valid Unit
* invalid Unit title
* valid Lesson
* invalid Lesson title
* valid positions
* invalid positions if applicable
* valid status
* invalid status transition if applicable
* parent relationship invariants

### Application

* create course
* update course
* list courses
* get course with structure
* delete course
* add/update/delete unit
* add/update/delete lesson
* not found
* invalid input
* duplicate positions
* ordering changes
* invalid parent relationships

### Authorization

Verify:

* Teacher with `Course.View` can read.
* Teacher with `Course.Manage` can mutate.
* Teacher without required permission is denied.
* Student principal cannot access Teacher management endpoints.
* Existing teacher/student principal separation remains intact.

### Tenant isolation

Explicitly test:

* Tenant A cannot read Tenant B course.
* Tenant A cannot update Tenant B course.
* Tenant A cannot delete Tenant B course.
* Tenant A cannot add a Unit to Tenant B course.
* Tenant A cannot modify Tenant B Unit.
* Tenant A cannot add a Lesson to Tenant B Unit.

Also verify:

* correct PublicId + wrong slug → existing 301 behavior
* invalid PublicId → existing 404 behavior
* anonymous protected request → existing 401 behavior

Use Testcontainers PostgreSQL for integration coverage.

---

# Important architectural constraint

Do NOT reintroduce global JWT tenant binding.

An authenticated teacher may still browse public resources belonging to another Teacher Platform in future.

Tenant-scoped management authorization must be enforced by:

Authentication
→ Tenant Resolution
→ Permission Authorization
→ Application membership/tenant guard

Maintain the architecture already approved in Task 1 and Task 2.

---

# Implementation workflow

Implement incrementally:

1. Domain
2. Infrastructure / EF / migration
3. Application
4. API
5. Tests
6. Verification
7. Documentation
8. Git commits

Use small logical commits consistent with previous tasks.

After each major phase, verify compilation/tests before continuing where practical.

---

# Documentation

Update:

* `IMPLEMENTATION_PLAN.md`
* Task 3 documentation according to the existing project documentation convention

Record the approved decisions, especially:

* Draft/Published
* no Course Slug
* no Course PublicId
* duplicate titles allowed
* hard delete/cascade
* explicit ordering
* tenant ownership validation
* public student browsing is out of scope

Do not silently add future requirements.

---

# Final verification requirements

Before reporting completion:

* `dotnet build --warnaserror`
* Full Unit test suite
* Full Integration test suite
* Verify migration
* Verify Docker/Testcontainer integration behavior where applicable
* Verify tenant isolation
* Verify authorization
* Verify existing Task 1 and Task 2 behavior has not regressed
* Verify working tree state

Do NOT push.

---

# STOP condition

When Task 3 is complete, STOP.

Report:

1. What was implemented
2. Changed files
3. Domain decisions implemented
4. Database/migration changes
5. API endpoints
6. Authorization behavior
7. Tenant-isolation behavior
8. Tests and exact results
9. Build result
10. Commit hashes
11. Git status
12. Any remaining risks or deviations from this authorization

Do not begin Task 4.

Wait for human review and approval.

---

# Post-review implementation addendum

## Approved deviation (Tasks/Approved1.md)

The Persistence section above required DB-level uniqueness on `(CourseId, Position)` and
`(UnitId, Position)`. Final implementation changed this after approval.

> DB-level unique constraints on CourseId+Position and UnitId+Position were intentionally not used
> because EF Core's change-tracking/topological ordering conflicts with atomic reordering when those
> unique indexes are modeled as immediate uniqueness constraints. Ordering invariants are therefore
> enforced by the Course/Unit domain aggregates, which are the single writers of ordering state.

This is a **deliberate deviation** from the original Task 3 planning wording.

The Course/Unit aggregate is the single writer for ordering; domain logic guarantees unique and
contiguous positions; no application path bypasses the aggregate to set `Position`; reordering stays
atomic through the existing `IUnitOfWork`/transaction. No deferrable PostgreSQL constraints,
hand-managed migration SQL, or EF snapshot hacks were introduced, and no replacement abstraction was
added to compensate.

Migration `20260903014302_AddCourseContent` creates `courses`/`units`/`lessons` with non-unique
position lookup indexes (positions are enforced by the aggregate).
