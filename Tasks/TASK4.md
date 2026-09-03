# Task 4 — Planning Only

## Student Enrollment in Teacher Courses

Task 3 has been officially reviewed and APPROVED.

Current approved state:

* Steps 0–8: complete and approved.
* Task 1 — Teacher Platform Management: complete and approved.
* Task 2 — Central Student Identity & Following: complete and approved.
* Task 3 — Teacher Platform Course Content: complete and approved.
* Current architecture, domain rules, tenant model, authentication model, permission model, and API conventions are already established.
* Working tree is clean.
* Nothing should be pushed.

## IMPORTANT: Do NOT redesign previously approved decisions

This planning phase is NOT a request to rediscover the product architecture.

The project has already gone through extensive product/domain discussions and previous tasks established the foundation.

You MUST treat the existing documentation and approved Task 1–3 implementation as the source of truth.

Before proposing anything, read and reconcile:

1. `AGENTS.md`
2. `IMPLEMENTATION_PLAN.md`
3. Task 1 documentation
4. Task 2 documentation
5. Task 3 documentation
6. Any existing approved decision/approval documents
7. Current Domain/Application/Infrastructure/API code
8. Existing tests

Do not ask questions whose answers are already explicitly established in the documentation or previous approved tasks.

Do not reopen previously decided architecture unless you discover a real implementation conflict.

---

# Expected Task 4 direction

Based on the established project sequence, Task 4 is expected to be:

**Student Enrollment in Teacher Courses**

Task 3 created:

Teacher Platform
→ Course
→ Unit
→ Lesson

Task 2 created:

Central Student
→ Following
→ Teacher

Task 4 should therefore investigate and plan the missing academic relationship:

**Student → Enrollment → Course**

However, do NOT blindly assume missing business rules.

First verify the existing documentation and implementation.

If the approved documentation establishes Enrollment as the next capability, use that as the source of truth.

If you discover that the existing roadmap explicitly defines a different Task 4, report that instead and reconcile it with the current implementation.

---

# Critical conceptual distinction

Preserve the distinction already established in the system:

**Following ≠ Enrollment**

Following is a central relationship between a Student and a Teacher.

Enrollment is an academic relationship between a Student and a Course.

A Student may follow a Teacher without enrolling in a course.

An Enrollment must not automatically be inferred merely because a Student follows a Teacher.

Do not collapse these concepts into one relationship.

Likewise:

* Student identity remains central.
* Teacher Platform/Course remains tenant-scoped.
* Enrollment must respect the tenant boundary of the Course.
* A student may interact with multiple teachers/platforms through the same central identity.

---

# What the planning phase must determine

Produce a complete Task 4 implementation plan, but distinguish carefully between:

### A. Already approved decisions

Extract these from the existing documentation and state them briefly.

Do NOT ask the human to approve them again.

### B. New decisions genuinely required for Enrollment

Only these should be presented as questions/recommendations for human approval.

---

# 1. Task objective

Explain:

* What Enrollment introduces.
* Why it comes after Student Identity/Following and Course Content.
* Which bounded context owns Enrollment.
* Whether Enrollment itself is tenant-scoped.
* How it relates to central Student identity and tenant-scoped Course.

The plan should make the data ownership clear.

---

# 2. Enrollment domain model

Determine the appropriate Enrollment entity based on the existing domain conventions.

At minimum investigate:

* Id
* StudentId
* CourseId
* TenantId
* status/lifecycle if required
* enrollment date/time
* audit fields

Do not add fields merely because they might be useful someday.

Do not introduce payment fields unless the approved documentation already requires them.

Future payment/wallet/purchase functionality is separate unless explicitly documented as part of Enrollment.

---

# 3. Enrollment lifecycle

This is an important area.

Check the existing documentation for previously defined rules regarding enrollment state.

Determine whether the system already specifies concepts such as:

* Active
* Cancelled
* Completed

or another lifecycle.

If the lifecycle is NOT already defined, identify it as a NEW business decision.

Do not invent a complex lifecycle.

In particular, investigate the existing project rule that course completion is not simply "the student finished all lessons"; completion is tied to the academic year / final exam period and teacher course closure.

If this rule affects Enrollment status, explain the relationship carefully.

Do NOT implement course completion logic in Task 4 unless the approved scope explicitly requires it.

---

# 4. Enrollment eligibility

Investigate the already approved business rules and determine exactly when a Student can enroll.

Specifically inspect whether existing decisions establish:

* Draft vs Published Course
* Following requirement
* Teacher approval
* Student self-enrollment
* Enrollment by Teacher/Admin
* Enrollment in inactive/deactivated platforms
* Enrollment in a course belonging to another tenant

Do not assume that Following is required for Enrollment.

If the documentation does not answer one of these questions, mark only that item as a decision requiring approval.

---

# 5. Duplicate enrollment

Determine whether one Student can have multiple active enrollments in the same Course.

Prefer a simple domain/database rule if the existing business model supports it.

If the business rule is already documented, use it without asking again.

If not documented, provide a recommendation and mark it for approval.

---

# 6. Tenant isolation

This is mandatory.

Enrollment connects:

Central Student
+
Tenant-scoped Course

The plan must explain exactly how tenant isolation works.

A student may have enrollments in courses belonging to multiple Teacher Platforms.

Therefore:

* Enrollment must carry the tenant context necessary for safe tenant-scoped operations if consistent with the existing model.
* Course ownership must always be validated.
* Student identity must remain central.
* Tenant query filters must not accidentally make a central student invisible.
* Cross-tenant access must never expose another tenant's enrollment/course data.

Do not reintroduce global JWT tenant binding.

A Student JWT must remain central and capable of interacting with multiple Teacher Platforms.

---

# 7. Authorization model

Build on the existing principal architecture.

Determine:

### Student

What enrollment operations can a Student perform?

For example:

* create/self-enroll
* view own enrollment
* cancel own enrollment

Only include operations actually justified by the existing business rules.

### Teacher

What enrollment information/actions can the Teacher perform?

For example:

* view enrolled students
* approve/reject if such a workflow exists
* manage enrollment

Again, do not invent a teacher workflow.

### Admin / Assistant

Use the existing dynamic permission system.

Do not create specialized roles.

If new permissions are required, propose the minimum permission codes.

Do not grant enrollment permissions merely because someone is an Assistant.

---

# 8. API contract

Propose the smallest necessary API surface.

For every endpoint specify:

* HTTP method
* route
* principal type
* permission
* request DTO
* response DTO
* tenant behavior
* expected errors

Respect the existing route conventions.

Important:

Student central identity routes remain under `/api/student/...`.

Teacher Platform management routes remain under:

`/{publicId}/{slug}/api/platform/...`

If an Enrollment endpoint needs a different route because it represents a Student action versus Teacher management, explain why.

Do not create public course URLs in Task 4.

---

# 9. Application services

Follow the established one-service-per-use-case pattern.

Propose only the services actually needed.

Examples may include:

* EnrollStudentService
* GetStudentEnrollmentService
* ListStudentEnrollmentsService
* CancelEnrollmentService

and potentially teacher-side services if the approved business rules require them.

Do NOT automatically create all of these.

Determine the minimum required set from the business rules.

Use existing repository and UnitOfWork conventions.

Do not introduce:

* CQRS
* MediatR
* generic repositories
* event bus
* microservices
* unnecessary abstractions

---

# 10. Persistence

Determine:

* Enrollment table
* foreign keys
* indexes
* unique constraints
* tenant filtering
* cascade/restrict delete behavior

Pay special attention to Course deletion.

Task 3 currently allows hard deletion/cascade because no Enrollment data existed yet.

Now Enrollment introduces a real student relationship.

Therefore the Task 4 plan MUST explicitly address:

> What should happen if a Course has existing Enrollments and someone attempts to delete the Course?

Do not silently inherit Task 3's previous cascade-delete behavior if it would destroy academic records.

This is a genuinely important Task 4 business/data decision if it was not already established.

Likewise consider what happens to Enrollment records when:

* Course changes from Published → Draft
* Course is published
* Teacher Platform is deactivated
* Unit/Lesson content changes

Do not implement unrelated completion/progress behavior, but identify any direct Enrollment impact.

---

# 11. Course visibility and publication

Task 3 introduced:

* Draft
* Published

Determine from existing documentation how this interacts with Enrollment.

If not previously defined, present the minimum required rule for approval.

Do NOT introduce public student course browsing as part of Task 4 unless the existing roadmap explicitly requires it.

Do not confuse:

* Course existence
* Course visibility
* Enrollment eligibility
* Course access
* Lesson completion

These are separate concepts.

---

# 12. Tests

Plan tests for:

### Domain

* valid enrollment
* duplicate enrollment
* invalid enrollment
* lifecycle rules if applicable

### Application

* student self-enrollment if approved
* own enrollment retrieval
* teacher enrollment visibility if approved
* cancellation if approved
* course eligibility
* not found
* invalid state
* cross-tenant references

### Authorization

* Student can perform only approved student actions.
* Teacher can perform only approved teacher actions.
* Assistant follows explicit permissions.
* Student cannot access teacher management endpoints.
* Teacher cannot impersonate or manipulate another tenant's enrollment.
* Existing principal_type behavior remains unchanged.

### Integration

Use Testcontainers PostgreSQL.

Explicitly test:

* Student enrolling in valid course.
* Duplicate enrollment.
* Draft/published behavior.
* Cross-tenant course reference.
* Student's enrollments across multiple Teacher Platforms.
* Teacher sees only their platform's enrollments.
* Tenant A cannot access Tenant B enrollment data.
* Anonymous → 401.
* Missing permission → 403.
* Unknown resources → 404.
* Existing Tasks 1–3 regression suite remains green.

---

# 13. Important domain/data question: Course deletion

This MUST be explicitly analyzed.

Task 3 allowed:

Course → hard delete → Units/Lessons cascade.

With Enrollment introduced, determine whether the correct behavior should now be:

Option A:

* Prevent Course deletion once enrollments exist.

Option B:

* Soft-delete/archive Course.

Option C:

* Keep hard delete and cascade Enrollment.

Option D:

* Another rule already established by the documentation.

Do NOT choose silently.

If this rule is already documented, follow it.

If not, recommend the least destructive option that preserves academic records and explain why.

---

# 14. Explicit non-goals

Unless existing approved documentation says otherwise, Task 4 must NOT implement:

* Payments
* Wallet
* Course pricing
* Coupons
* Payment gateways
* Student progress
* Lesson completion
* Exams
* Homework
* Revision
* Grades
* Attendance
* Certificates
* Reviews
* Ratings
* Notifications
* Analytics
* Media upload/playback
* Public course browsing
* Complex course access entitlement system

Enrollment should establish the academic relationship only.

---

# 15. Migration impact

Describe the expected migration and all required constraints/indexes.

Do not write the migration during planning.

---

# 16. Risks and genuinely unresolved decisions

List ONLY issues that are not already resolved by the existing approved documentation.

For each unresolved issue provide:

* Question
* Recommended option
* Reason
* Consequences

Do not reopen old decisions.

---

# 17. Expected implementation sequence

Propose small logical commits:

1. Domain
2. Infrastructure / persistence / migration
3. Application
4. API / authorization
5. Tests
6. Documentation
7. Verification

Keep commits small and consistent with Tasks 1–3.

---

# STRICT PLANNING-ONLY RULE

Do NOT implement anything.

Do NOT:

* modify source code
* create migration
* modify database
* add tests
* change API
* change permissions
* commit
* push

The only allowed output is the Task 4 implementation plan and the genuinely unresolved decisions requiring human approval.

At the end, report:

1. Task 4 title
2. Why it is next
3. Existing approved rules reused
4. Proposed domain model
5. Enrollment lifecycle
6. Eligibility rules
7. Tenant model
8. Authorization
9. API
10. Application services
11. Database/migration
12. Tests
13. Explicit non-goals
14. ONLY genuinely unresolved decisions
15. Proposed implementation commits

Then STOP and wait for human approval.

Do not start Task 4 implementation until explicit approval is given.
