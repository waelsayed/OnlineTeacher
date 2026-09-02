## Task 2 — Implementation Authorization

The Task 2 implementation plan has been reviewed and approved.

Proceed with **Task 2 — Central Student Identity & Following** exactly according to the approved plan you just provided.

### Approved decisions

1. Add `principal_type` to JWTs:

   * Teacher token → `principal_type=teacher`
   * Student token → `principal_type=student`

2. Do NOT add `PublicId` to Student.

   * Student identity uses internal `Guid` as `sub`.
   * Students have no public URL.

3. Student central API routes:

   * `POST /api/student/register`
   * `POST /api/student/login`
   * `GET /api/student/me`
   * `POST /api/student/follow/{teacherPublicId}`
   * `DELETE /api/student/follow/{teacherPublicId}`
   * `GET /api/student/following`
   * `GET /api/student/following/{teacherPublicId}`

4. Student endpoints must require an authenticated Student principal.

   * Implement this using the existing authorization architecture with a minimal principal-type policy/handler.
   * Do not create a second authentication system.
   * Teacher JWT must not satisfy Student-only endpoints.
   * Student JWT must not satisfy Teacher-only management endpoints.

5. StudentFollow is a central, non-tenant-scoped relationship.

   * No `TenantId`.
   * No tenant query filter.
   * A Student can follow multiple Teachers.

6. Duplicate Student↔Teacher follows must be protected by a database unique constraint.

   * Do not rely only on application-level duplicate checks.
   * Return the existing API's appropriate business/conflict status according to established project conventions.
   * Do NOT arbitrarily introduce a new status-code convention.

7. Self-follow protection is a domain rule.

   * Keep the defensive rule even though Student and Teacher are currently separate entities/tables.

### Critical architectural guarantees

Do NOT change the existing architecture.

Preserve all existing behavior:

* Teacher login still requires `Email + Password + PublicId`.
* Teacher JWT behavior remains compatible, with only the approved `principal_type=teacher` addition.
* Existing Teacher Platform management remains unchanged.
* Existing dynamic permission system remains unchanged.
* Existing tenant resolution remains unchanged.
* Existing canonical slug `301` behavior remains unchanged.
* Existing public cross-tenant browsing remains allowed.
* Do NOT reintroduce global JWT-to-tenant binding.
* Student JWT must not receive Teacher platform permission claims.
* Student identity and following remain central.
* Tenant-owned data remains tenant-isolated.

### Implementation sequence

Work incrementally in logical phases:

1. Domain
2. Persistence / EF Core / migration
3. Application services/use cases
4. JWT + authorization
5. API endpoints
6. Unit tests
7. Integration tests
8. Full verification
9. Documentation
10. Git commits

At each phase:

* Inspect existing implementation first.
* Make the smallest change necessary.
* Follow existing project conventions.
* Do not introduce unnecessary abstractions.
* Do not implement future features.

### Important scope boundary

DO NOT implement:

* Courses
* Enrollment
* Purchases
* Payments
* Wallet
* Lessons
* Media/files
* Student dashboard
* Attendance
* Grades
* Exams
* Messaging
* Notifications
* Reviews/ratings
* Analytics
* Custom RBAC
* Any unrelated feature

This task is strictly:

**Central Student Identity + Authentication + Student Profile + Teacher Following**

### Tests

Implement all tests specified in the approved Task 2 plan.

In particular verify:

* Student registration/authentication.
* Student JWT and principal type.
* Teacher JWT remains valid.
* Student cannot access Teacher management APIs.
* Teacher cannot access Student-only APIs.
* Student can follow multiple Teachers.
* Duplicate follow is protected at DB level.
* Unfollow works correctly.
* Student can browse public Teacher A and Teacher B.
* Student remains unable to manage either platform.
* Existing Teacher Platform tests remain green.
* Existing tenant isolation tests remain green.
* Existing public cross-tenant browsing tests remain green.

Run:

* `dotnet build --warnaserror`
* Full `dotnet test`

Do not weaken existing tests.

### Database

Create the required migration and verify it against the existing PostgreSQL/Testcontainers setup.

Pay particular attention to:

* Student tables being central/non-tenant-scoped.
* StudentFollow being central/non-tenant-scoped.
* Correct foreign keys.
* Correct indexes.
* Unique `(StudentId, TeacherId)` constraint.
* Existing tenant query filters remaining unchanged for tenant-owned entities.

### API

Use DTOs at the API boundary.

Controllers remain thin.

Business rules belong in Domain/Application.

Use the existing ProblemDetails/error handling.

Do not expose EF entities.

Do not expose password hashes.

Do not place sensitive student data into JWT claims.

### Git

Use small logical commits.

Do NOT rewrite existing history.

Do NOT push to `origin`.

Keep the working tree clean.

### Decision escalation

If you discover a requirement that is not covered by the approved plan, **STOP and ask for my decision before implementing it**.

Do not silently invent a new architectural rule.

Minor implementation details that are clearly covered by the approved plan can be decided using existing project conventions.

### Completion

When Task 2 is fully implemented and verified, provide a complete report containing:

1. What was implemented.
2. Domain model changes.
3. Database/migration changes.
4. Application services/use cases.
5. JWT/authentication changes.
6. Authorization/principal-type implementation.
7. All API endpoints.
8. Follow/unfollow business rules.
9. Tenant-isolation behavior.
10. Public cross-tenant browsing verification.
11. All changed files.
12. Unit test results.
13. Integration test results.
14. Build result.
15. Migration/database verification.
16. Commit hashes.
17. Final `git status`.
18. Any risks or decisions made.

Then **STOP**.

Do NOT start Task 3 or any other future feature.

Wait for explicit human review and approval.
