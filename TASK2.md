### Task 2 — Student Identity & Following

Steps 0–8 and Task 1 — Teacher Platform Management are completed and approved.

Current baseline:

* Latest Task 1 commit: `c1ff6bf`
* Working tree is clean.
* Existing authentication, authorization, tenant resolution, tenant isolation, platform management, Docker, and testing infrastructure are established.
* Do NOT redesign the existing architecture.

Your task is **Task 2 only — Central Student Identity & Teacher Following**.

The goal is to introduce the central Student identity and the Student ↔ Teacher relationship without implementing Courses, Enrollment, Payments, or Content management yet.

---

# 1. Read Before Coding

Read and understand:

* `AGENTS.md`
* `IMPLEMENTATION_PLAN.md`
* Product/domain documentation
* Existing Domain, Application, Infrastructure, API, UnitTests, and IntegrationTests
* Existing Teacher/TeacherPlatform identity and membership model
* Existing JWT authentication and authorization
* Existing tenant resolution and cross-tenant public browsing behavior
* Task 1 implementation and tests

Do not assume missing business rules.

Before coding, inspect the existing model and determine the minimum changes required to introduce the Student identity cleanly.

---

# 2. Core Architectural Rule

The Student identity is **CENTRAL**, not tenant-specific.

A single Student account must be able to interact with multiple Teacher Platforms using the same central identity.

Conceptually:

`Central Student`
→ follows Teacher A

`Central Student`
→ follows Teacher B

The Student must NOT have a separate account per Teacher Platform.

Do NOT create a `TenantStudent` identity as a replacement for the central Student identity.

The existing tenant model remains valid for Teacher Platform management.

---

# 3. Student Identity

Implement the minimum complete Student identity capability.

A Student should have:

* Internal database ID.
* Stable PublicId where appropriate according to existing identity conventions.
* Name/profile information required by the existing domain documentation.
* Email as the login identifier if that is the established identity approach.
* Secure password handling using the existing password hashing abstraction.
* Created/updated audit information consistent with the existing model.

Do NOT store plaintext passwords.

Do NOT expose password hashes through DTOs or API responses.

Do not invent unnecessary profile fields.

If the existing documentation specifies exact Student fields, follow it.

---

# 4. Student Registration

Implement Student registration.

Requirements:

* Register a Student using the central identity system.
* Validate required fields.
* Normalize/validate email according to the existing Email value-object/conventions.
* Hash the password using the existing password hashing abstraction.
* Reject duplicate Student email according to the established uniqueness/business rules.
* Do not leak whether an email exists through inappropriate authentication/error responses where that would create an account-enumeration issue.

Use the existing exception/ProblemDetails conventions.

Add appropriate unit and integration tests.

---

# 5. Student Authentication

Implement Student login using the existing JWT infrastructure.

The Student must be able to authenticate centrally.

Important distinction:

### Teacher authentication

Existing Teacher login requires:

`Email + Password + PublicId`

because the Teacher is entering a specific Teacher Platform context.

### Student authentication

Student identity is central.

Therefore Student login must NOT require a Teacher Platform PublicId.

Student authentication should produce a JWT identifying the Student.

Use an appropriate claim to distinguish the authenticated principal type, rather than relying on ambiguous interpretation of `sub`.

For example, use the existing claim conventions if available, or introduce the minimum necessary principal-type claim such as:

`principal_type = student`

Do not introduce a completely separate authentication mechanism.

Do not break existing Teacher JWT behavior.

---

# 6. JWT Security

Student tokens must contain only the claims necessary for authentication/authorization.

At minimum, the system must be able to distinguish:

* Teacher principal
* Student principal

Do not put sensitive student data into JWT claims.

Do not put password/hash into JWT.

Do not put unnecessary profile information into JWT.

Student tokens must not automatically grant Teacher Platform management permissions.

A Student must never be treated as a Teacher merely because both use JWT authentication.

Add unit tests for Student JWT construction.

---

# 7. Student ↔ Teacher Following

Implement the central following relationship.

Business rule:

A Student may follow multiple Teachers.

A Teacher may have many Students following them.

The relationship is independent of tenant identity.

Conceptually:

`Student 1 → Teacher A`
`Student 1 → Teacher B`
`Student 2 → Teacher A`

Do NOT model this as a tenant-local student account.

---

# 8. Follow / Unfollow

Implement APIs/use cases for:

* Follow a Teacher.
* Unfollow a Teacher.
* Check whether the current Student follows a Teacher.
* List teachers followed by the current Student.

Use the existing API/application-service conventions.

### Rules

* Only an authenticated Student can follow/unfollow.
* A Student cannot follow the same Teacher more than once.
* Following the same Teacher twice must be handled safely/idempotently according to the established API conventions.
* Unfollowing a Teacher the Student does not follow must not corrupt data.
* A Student cannot follow themselves if the identity model technically permits the Student/Teacher records to overlap; enforce the correct domain rule instead of relying on accidental database behavior.
* Follow/unfollow must not require Teacher Platform membership.
* Following a Teacher must not grant access to private Teacher Platform management APIs.

Do not add enrollment behavior yet.

---

# 9. Public Teacher Browsing

This is a critical requirement.

An authenticated Student must be able to browse public Teacher Platforms.

An authenticated Student must be able to access public information for Teacher A and Teacher B using the same central account.

The existing rule remains:

**Authentication does NOT globally bind the JWT to the tenant route.**

Therefore:

Student JWT
→ Teacher A public platform → allowed

Student JWT
→ Teacher B public platform → allowed

Anonymous visitor
→ public Teacher Platform → allowed where the endpoint is public

But:

Student JWT
→ protected Teacher Platform management endpoint → `403`

Do NOT reintroduce the old global JWT tenant mismatch middleware.

Do NOT weaken the existing protected management authorization.

---

# 10. Student Access to Protected Teacher APIs

A Student must NOT gain Teacher permissions.

For example:

Student JWT → `/api/{teacherPublicId}/{slug}/api/platform/me`

must not return Teacher management information merely because the Student is authenticated.

Expected result should be the appropriate unauthorized/forbidden response according to the existing authorization pipeline.

Verify this with integration tests.

---

# 11. Teacher ↔ Student Visibility

Implement only the relationship capability required by this task.

Teacher Platform management may need to know whether a Teacher has followers.

If a minimal read endpoint is necessary, expose only the minimum required information.

Do NOT build:

* Student management dashboard
* Student analytics
* Course enrollment
* Payment history
* Attendance
* Grades
* Messaging
* Notifications

Those belong to later tasks.

---

# 12. Data Model & Persistence

Use PostgreSQL/EF Core and the existing persistence architecture.

The central Student entity and Follow relationship must NOT be tenant-scoped.

Be very careful with the existing:

* `ITenantScoped`
* TenantContext
* EF global query filters

Student identity and Student↔Teacher follow data should remain accessible across Teacher Platforms where the business operation requires it.

Do NOT accidentally apply the current tenant query filter to the central Student identity.

At the same time, do not disable tenant filtering globally.

Use explicit central/application-level access where appropriate.

Add the necessary indexes/unique constraints.

The follow relationship should have a database-level uniqueness guarantee for:

`Student + Teacher`

Do not rely only on application-level duplicate checking.

---

# 13. API Design

Follow the existing API conventions.

Use DTOs.

Never expose EF entities directly.

Keep controllers thin.

Use one application service/use case per meaningful operation.

Do not introduce generic repositories.

Do not introduce CQRS/MediatR/event bus.

Suggested API surface, adapt to existing conventions if a better established route structure exists:

### Student identity

* `POST /api/student/register`
* `POST /api/student/login`
* `GET /api/student/me`

### Following

* `POST /api/student/follow/{teacherPublicId}`
* `DELETE /api/student/follow/{teacherPublicId}`
* `GET /api/student/following`
* `GET /api/student/following/{teacherPublicId}`

These are suggestions, not permission to create an inconsistent API style. Follow the existing routing conventions.

Teacher PublicId is the stable public identity already used by the platform.

Do not use internal database IDs in public URLs.

---

# 14. Authorization

Introduce Student-specific authorization only where necessary.

The authorization model must distinguish:

* Teacher
* Student

Do not give Student users any Teacher role or platform-management permission.

For Student endpoints:

* registration/login → anonymous
* `/student/me` → authenticated Student
* follow/unfollow/list following → authenticated Student

Teacher management endpoints remain unchanged.

If a shared policy mechanism is needed, extend the existing authorization architecture minimally.

Do not replace the existing permission system.

---

# 15. Tenant Isolation Rules

This task must preserve the following model:

### Central operations

Student identity and following are central.

### Tenant operations

Teacher Platform management remains tenant-scoped.

### Public operations

Public Teacher Platform browsing can be cross-tenant.

Therefore:

* Student A can follow Teacher A.
* Student A can follow Teacher B.
* Student A can browse Teacher A public platform.
* Student A can browse Teacher B public platform.
* Student A cannot manage Teacher A platform.
* Student A cannot manage Teacher B platform.
* Teacher A cannot manage Student identity as part of this task unless an explicitly required read relationship is implemented.
* Teacher A must not gain access to Student B's central account.

---

# 16. Validation & Business Rules

Implement proper validation for:

* Student name.
* Email.
* Password.
* Follow target Teacher.
* Duplicate follow.
* Unfollow behavior.

Use existing value objects and validation conventions where applicable.

Do not duplicate validation unnecessarily.

---

# 17. Tests

Add comprehensive tests.

## Unit tests

At minimum cover:

* Student registration success.
* Invalid student data.
* Duplicate student email.
* Password hashing.
* Student authentication success.
* Student authentication failure.
* Student JWT claims.
* Follow success.
* Duplicate follow behavior.
* Unfollow.
* Unfollow when not following.
* Multiple teachers followed by one student.
* Student/Teacher principal distinction.
* Student cannot satisfy Teacher-only authorization.

## Integration tests

Use the existing PostgreSQL/Testcontainers setup.

At minimum verify:

1. Student can register.
2. Student can login without a Teacher Platform PublicId.
3. Student can retrieve `/student/me`.
4. Student can follow Teacher A.
5. Student can follow Teacher B.
6. Student's following list contains both teachers.
7. Duplicate follow is handled correctly.
8. Student can unfollow a teacher.
9. Student A can access public Teacher A platform.
10. Student A can access public Teacher B platform.
11. Student cannot access Teacher Platform management endpoint.
12. Student cannot manage another tenant.
13. Teacher authentication continues to work unchanged.
14. Existing Teacher Platform management tests remain green.
15. Existing public cross-tenant browsing tests remain green.
16. Existing tenant isolation tests remain green.
17. Database uniqueness prevents duplicate Student↔Teacher follow relationships.

Run the FULL test suite.

Do not weaken existing tests.

---

# 18. Backward Compatibility

Do not break:

* Teacher login.
* Teacher JWT claims.
* Platform management.
* Dynamic permissions.
* Tenant route resolution.
* Canonical slug redirects.
* Public cross-tenant browsing.
* Docker.
* `/health`.

If existing JWT claims need a minimal extension to distinguish principal types, preserve all existing Teacher claims and behavior.

---

# 19. Documentation

Update the relevant documentation.

Document clearly:

* Student is a central identity.
* Student is not tenant-scoped.
* One Student can follow multiple Teachers.
* Following does not equal enrollment.
* Following does not grant private course/platform access.
* Authenticated Students may browse public Teacher Platforms cross-tenant.
* Student JWT does not grant Teacher permissions.

Update `IMPLEMENTATION_PLAN.md` and create a task document such as `TASK2.md` if consistent with the repository convention.

---

# 20. Strict Scope Boundary

DO NOT implement:

* Courses
* Course sections
* Lessons
* Enrollment
* Purchases
* Payments
* Wallet
* Content management
* Video/media
* Student dashboard
* Attendance
* Grades
* Exams
* Messaging
* Notifications
* Reviews/ratings
* Recommendations
* Teacher analytics
* Admin RBAC UI

This is **Student Identity & Following only**.

Do not expand the scope.

---

# 21. Git

Use small logical commits where appropriate.

Do not rewrite existing Git history.

Do not push to `origin`.

Keep the working tree clean when finished.

---

# 22. Completion Criteria

Task 2 is complete only when:

* Central Student identity exists.
* Student registration works.
* Student login works without PublicId.
* Student JWT is distinguishable from Teacher JWT.
* Student profile endpoint works.
* Follow/unfollow works.
* One Student can follow multiple Teachers.
* Duplicate follow is protected at database level.
* Public cross-tenant browsing still works.
* Student cannot access Teacher management APIs.
* Existing Teacher authentication remains intact.
* Tenant isolation remains intact.
* Unit tests pass.
* Integration tests pass.
* Full build passes with `--warnaserror`.
* Documentation is updated.
* No secrets are introduced.
* Working tree is clean.
* Changes are committed.

---

# STOP CONDITION

This is **Task 2 only**.

After completing it:

1. Report exactly what was implemented.
2. List all changed files.
3. Explain the Student data model.
4. Explain how Student identity remains central/non-tenant-scoped.
5. List all Student API endpoints.
6. Explain Student JWT claims/principal distinction.
7. Explain Follow/Unfollow behavior and uniqueness protection.
8. Explain how cross-tenant public browsing was preserved.
9. Explain how Student access to protected Teacher APIs is prevented.
10. Report unit-test results.
11. Report integration-test results.
12. Report `dotnet build --warnaserror` result.
13. Report commit hash(es).
14. Report final `git status`.
15. Report any risks or architectural decisions.

Then **STOP**.

Do NOT continue to Courses, Enrollment, Content, Payments, Media, Dashboard, or any other future task.

Wait for my explicit review and approval.
