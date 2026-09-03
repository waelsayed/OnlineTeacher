# AGENTS.md

# Online Teacher — AI Development Agent Instructions

## 1. Project Overview

You are working on **Online Teacher**, a SaaS educational platform developed by **ProNileSoft**.

The platform is designed specifically for **school teachers**, not trainers.

The system consists of:

* A **Central Platform** operated by ProNileSoft.
* Multiple independent **Teacher Platforms**.
* Students use one central identity and can interact with multiple teachers.
* Each Teacher Platform represents an independent tenant.

The repository contains the project's technical and product documentation.

**The documentation is the primary source of truth for product behavior, business rules, architecture, and requirements.**

Do not invent functionality that is not documented.

## Project Documentation Layout

Documentation responsibilities are intentionally kept separate:

* `AGENTS.md` — Project rules: how implementation work must be performed.
* `IMPLEMENTATION_PLAN.md` — Implementation phases and current project status.
* `Tasks/` — Individual implementation task documents.
* `PROJECT_DOCUMENTATION/` — The project's **reference library** containing the original system
  analysis, business analysis, requirements, architecture, domain analysis/model, use cases, business
  rules, security architecture, multi-tenancy, and other important system/design documents.
* `PROJECT_IMPLEMENTATION_HISTORY.md` — Records what was actually implemented and how the project evolved.

`PROJECT_DOCUMENTATION/` contains the **original project documentation**. Some documents are in Arabic,
which is intentional; they are the original documentation and must **not** be translated, replaced,
renamed, deleted, or reorganized. Treat them as **reference/context material** (understanding the system
vision, requirements, domain concepts, architecture, security, and multi-tenancy model), **not** as
implementation instructions. Original approved implementation decisions and the current source code
represent implementation reality; the reference documents represent the original analysis/design context.

---

# 2. Technology Stack

The backend technology stack is fixed unless explicitly changed by the project owner.

## Backend

* ASP.NET Core Web API
* .NET 10
* C#
* PostgreSQL
* Docker / Docker Compose

The entire backend must be implemented using **ASP.NET Core Web API on .NET 10**.

Do not introduce:

* Node.js
* NestJS
* Express
* MongoDB
* MySQL
* SQL Server
* Microservices

unless explicitly requested and approved.

---

# 3. Database

The primary database is:

**PostgreSQL**

PostgreSQL must run inside a Docker container for local development.

Use Docker Compose to manage the database environment.

The project should provide a reproducible development environment so that a developer can start PostgreSQL without installing PostgreSQL directly on the host machine.

The database configuration must support environment-based configuration.

Do not hard-code:

* Database passwords
* Connection strings containing credentials
* Secrets
* API keys

Use environment variables or appropriate local development configuration.

---

# 4. Docker

Docker is required for the PostgreSQL development environment.

The project should include a Docker Compose configuration similar in concept to:

```text
docker-compose.yml
```

The exact configuration should be determined by the project structure and requirements.

The PostgreSQL container should have:

* Persistent volume
* Configurable database name
* Configurable username
* Configurable password
* Configurable port
* Health check when appropriate

The ASP.NET Core API may initially run directly from the development machine.

Do not containerize every component unnecessarily.

Keep the initial development environment simple.

---

# 5. Architecture Philosophy

Use a pragmatic layered architecture based on the project's documented architecture.

The architecture must provide clear separation between:

* Domain
* Application
* Infrastructure
* API / Presentation

Do not introduce unnecessary architectural complexity.

Avoid:

* Generic repositories without a real need
* Excessive abstractions
* Unnecessary interfaces
* Premature microservices
* Distributed systems
* Event buses without a concrete requirement
* Excessive CQRS complexity
* Over-engineered factories
* Abstractions created only to follow a design pattern

Every abstraction must have a clear purpose.

---

# 6. Project Structure

The final structure should follow the architecture documented in the repository.

A typical structure may be:

```text
src/
    OnlineTeacher.Api/
    OnlineTeacher.Application/
    OnlineTeacher.Domain/
    OnlineTeacher.Infrastructure/

tests/
    OnlineTeacher.UnitTests/
    OnlineTeacher.IntegrationTests/

docker/
```

However:

**Do not blindly create this structure if the existing documentation specifies a different structure.**

The existing architecture and repository structure take precedence.

---

# 7. Central Platform vs Teacher Platform

The system contains two major areas.

## Central Platform

Responsible for:

* Teachers
* Teacher Platforms
* Teacher subscriptions
* Subscription plans
* Platform activation
* Central administration
* Central permissions
* Teacher discovery
* Central coupons
* Teacher-level financial operations

## Teacher Platform

Responsible for:

* Students
* Courses
* Units
* Lessons
* Revision
* Exams
* Homework
* Enrollments
* Follow relationships
* Wallet
* Student payments
* Student coupons
* Posts
* Comments
* Messages
* Notifications
* Teacher team
* Dynamic permissions
* Student-related financial records

Central Platform and Teacher Platform must remain clearly separated at the architectural and authorization levels.

---

# 8. Multi-Tenancy

Teacher Platforms are tenants.

Tenant isolation is a critical security requirement.

Any tenant-owned data must be accessed within the correct tenant context.

The application must prevent:

```text
Teacher A → accessing Teacher B data
```

Tenant resolution must be explicit and testable.

Do not rely on developers remembering to manually add tenant filters everywhere.

Design the data-access/application architecture so tenant isolation is difficult to bypass accidentally.

---

# 9. Identity

A student has one central identity.

Example:

```text
Student
 ├── Teacher A
 ├── Teacher B
 └── Teacher C
```

Do not create a separate student account for every Teacher Platform.

Teacher Platform relationships such as:

* Follow
* Enrollment
* Wallet
* Course access

belong to the appropriate Teacher Platform context.

---

# 10. Public Teacher URL

Teacher Platforms have a stable public identifier separate from the internal database primary key.

The public URL may follow the structure:

```text
/{publicId}/{slug}
```

The public identifier must not expose a sequential database ID.

Routing must validate both:

```text
PublicId
+
Slug
```

Do not resolve a Teacher Platform using PublicId alone when the requested slug does not match the current canonical slug.

The exact canonical URL behavior must follow the documented architecture.

---

# 11. Authentication & Authorization

Authentication and authorization must be treated as separate concerns.

Authorization must support dynamic permissions.

Do not create dozens of specialized roles.

For example, avoid:

```text
PaymentAssistant
ExamAssistant
ContentAssistant
```

Instead use:

```text
Role
+
Permissions
```

An Assistant may have:

```text
Payment.Activate
```

while another Assistant may have:

```text
Exam.Create
Exam.Edit
File.Upload
```

Permissions should be enforced at the application/resource level, not only at the UI level.

Never rely on hiding buttons as an authorization mechanism.

---

# 12. Business Rules

Business rules must be implemented according to the project's documentation.

Important examples include:

## Follow

Enrollment in a course automatically creates the student's relationship with the teacher.

A student cannot manually remove that relationship while the enrollment requires it.

After the enrollment ends, the student may remove the follow relationship.

---

## Enrollment

Paid content requires payment/enrollment.

Free content does not require payment.

A student cannot purchase content when the Teacher Platform wallet balance is insufficient.

Do not automatically consume future wallet funds.

---

## Wallet

The student wallet belongs to the Teacher Platform.

The Central Platform does not own student wallets.

Wallet transactions must be auditable.

Financial records must never depend only on a mutable balance field.

---

## Coupons

Coupons are:

* Personal
* Assigned to one person
* Single-use
* Expirable
* Non-transferable
* Non-reusable after consumption

---

## Offline Students

An Offline Student is a student state.

It is not a Group.

Do not introduce group-assignment functionality unless explicitly required.

---

## Refunds

Student refunds are handled within the Teacher Platform.

The Central Platform is not financially responsible for student refunds.

Refunds must be recorded as financial events and must preserve the student's historical activity.

---

# 13. Database Design

Use PostgreSQL relational modeling.

Database design must follow the project's documented:

**Database Design & Data Architecture**

Do not redesign the database without reviewing the existing documentation.

Use:

* Foreign keys
* Unique constraints
* Check constraints where appropriate
* Proper indexes
* Transaction boundaries
* Concurrency protection
* Referential integrity

Financial and audit records should be treated as historical records and should not be casually deleted.

---

# 14. Entity IDs

Use appropriate internal identifiers for database entities.

Where an entity is exposed publicly, use a separate stable public identifier where required.

Do not expose internal sequential database IDs as public identifiers when the architecture specifies Public IDs.

Public identifiers must remain stable even if internal database implementation changes.

---

# 15. Financial Data

Financial operations require special care.

Wallet operations, purchases, coupon consumption, refunds, and balance changes must be transactional.

For example:

```text
Wallet Credit
      ↓
Balance Update
      ↓
Transaction Record
```

and:

```text
Purchase
      ↓
Validate Balance
      ↓
Consume Coupon if applicable
      ↓
Deduct Wallet
      ↓
Create Enrollment
      ↓
Record Financial Transaction
```

Operations that must succeed together should execute inside one database transaction.

Prevent:

* Negative balances
* Double spending
* Double coupon consumption
* Duplicate enrollment caused by retries

---

# 16. Idempotency & Concurrency

The API must be designed to handle repeated requests safely where appropriate.

Important operations include:

* Wallet activation
* Purchase
* Coupon consumption
* Enrollment
* Refund
* Exam submission

Use appropriate database constraints, transactions, locking, or idempotency mechanisms.

Do not assume the client will send a request only once.

---

# 17. API Design

Use RESTful ASP.NET Core Web API conventions.

API endpoints should be organized around business capabilities rather than database tables alone.

Do not expose EF entities directly as API responses.

Use DTOs.

Separate:

```text
Request DTO
Response DTO
Domain Entity
Persistence Model
```

where separation is actually useful.

Do not create DTOs or mappings unnecessarily for trivial internal operations.

---

# 18. Validation

Validation should happen at the appropriate application boundary.

Validate:

* Required fields
* Data formats
* Business constraints
* Authorization
* Tenant ownership
* State transitions

Do not rely exclusively on database exceptions to validate business rules.

---

# 19. Error Handling

The API must provide consistent error responses.

Do not return random exception messages directly to clients.

Handle:

* Validation errors
* Authentication failures
* Authorization failures
* Not found
* Business rule violations
* Concurrency conflicts
* Unexpected server errors

Sensitive internal exception details must not be exposed in production.

---

# 20. Logging

Use structured logging.

Logs should help diagnose:

* Authentication issues
* Authorization failures
* Tenant resolution problems
* Database failures
* External provider failures
* Background job failures
* Important business operations

Do not log:

* Passwords
* Tokens
* Secrets
* Sensitive personal data unnecessarily

---

# 21. Audit Trail

Important actions must be auditable.

Audit records should be able to identify:

* Actor
* Tenant
* Action
* Entity
* Entity ID
* Timestamp
* Session / Station
* IP address
* Relevant changes

Audit records are historical records.

Do not silently modify or delete them.

---

# 22. Session & Station Tracking

User activity should be associated with a session/station where appropriate.

A station identifier may be represented by a GUID.

The system should be able to track:

```text
User
Session
Station
IP
Device
Login
Activity
```

Deactivated users must immediately lose access while their historical records remain available.

---

# 23. Files & Videos

Do not store large files directly in PostgreSQL unless explicitly required.

Store file metadata in the database and use the appropriate file/object storage mechanism.

Video playback must use the project's Video Provider abstraction.

Do not tightly couple the domain model to one specific video provider.

Provider-specific information should remain inside the appropriate infrastructure/provider layer.

---

# 24. Background Processing

Use background processing only where the requirement actually needs asynchronous execution.

Examples may include:

* Notifications
* External provider synchronization
* File processing
* Cleanup
* Scheduled operations

Do not introduce a distributed job infrastructure unless required.

---

# 25. Testing

The project must contain automated tests.

At minimum, consider:

## Unit Tests

For:

* Domain rules
* Business rules
* Services
* Important state transitions

## Integration Tests

For:

* PostgreSQL
* API
* Authentication
* Authorization
* Tenant isolation
* Important transactions

Security-sensitive behavior must have tests.

Especially test:

```text
Tenant A cannot access Tenant B
```

---

# 26. Development Workflow

You must work incrementally.

Do not implement the entire system in one step.

Use this workflow:

```text
Inspect
  ↓
Understand
  ↓
Plan
  ↓
Implement
  ↓
Test
  ↓
Review
  ↓
Report
  ↓
Wait for Approval
```

---

# 27. Phase 0 — Project Discovery

Before writing code:

1. Read all available project documentation.
2. Inspect the repository structure.
3. Inspect existing source code.
4. Inspect configuration files.
5. Inspect existing database code.
6. Inspect existing tests.
7. Inspect Docker configuration.
8. Compare the implementation with the documentation.

Do not modify the project during Phase 0.

Produce a concise:

## Project Discovery Report

Include:

* Current project structure
* Existing technologies
* Existing architecture
* Implemented features
* Missing foundation
* Documentation/code mismatches
* Technical risks
* Recommended first implementation phase
* Decisions requiring human approval

Then stop.

---

# 28. Phase 1 — Foundation

After explicit approval, implement the project foundation.

The foundation should include only what is required for the first working vertical slice.

Potential components:

* ASP.NET Core Web API .NET 10
* Solution structure
* PostgreSQL Docker container
* Docker Compose
* Configuration
* Database connection
* Database migrations
* Logging
* Global error handling
* Authentication foundation
* Authorization foundation
* Tenant resolution foundation
* Basic automated testing

Do not implement unrelated business features during this phase.

---

# 29. First Vertical Slice

The first real vertical slice should validate the platform foundation.

Recommended flow:

```text
Teacher Registration
        ↓
Teacher Account
        ↓
Teacher Platform Creation
        ↓
Public ID
        ↓
Slug
        ↓
Platform Activation
        ↓
Teacher Login
        ↓
Tenant Resolution
        ↓
Authorized Teacher Platform Access
```

The goal is not to implement the entire Teacher Platform.

The goal is to prove that the core architecture works end-to-end.

---

# 30. Change Management

Before making significant architectural changes:

1. Identify the problem.
2. Explain why the current architecture is insufficient.
3. Propose the smallest reasonable change.
4. Explain its impact.
5. Stop and request human approval.

Do not silently change architectural decisions.

---

# 31. When Documentation and Code Conflict

Use this priority:

```text
Explicit Owner Decision
        ↓
Current Approved Architecture Documentation
        ↓
Current Business Rules
        ↓
Existing Code
        ↓
Agent Assumptions
```

Existing code is not automatically correct.

If the existing implementation contradicts approved documentation, report it.

Do not silently rewrite large portions of the system.

---

# 32. When Requirements Are Unclear

Classify decisions into three categories.

### A — Clearly documented

Implement directly.

### B — Implementation detail

Choose the simplest professional implementation and document the decision.

### C — Business or architectural decision

Do not guess.

Stop and request human approval.

---

# 33. Git Discipline

Keep changes small and reviewable.

Each implementation step should be logically isolated.

Avoid mixing:

```text
Architecture changes
+
Feature implementation
+
Unrelated refactoring
```

in one change.

Use clear commit messages.

Do not perform large unrelated refactoring unless explicitly requested.

---

# 34. Code Quality

Write production-quality C#.

Prefer:

* Clear naming
* Small focused classes
* Explicit dependencies
* Strong typing
* Async APIs where appropriate
* Proper cancellation support
* Testable code
* Clear boundaries

Avoid:

* Magic strings
* Hidden global state
* Static service abuse
* God classes
* God services
* Deep inheritance hierarchies
* Unnecessary abstractions

Follow modern ASP.NET Core and .NET 10 practices.

---

# 35. Agent Communication

After every meaningful implementation phase, report:

### Completed

What was implemented.

### Changed Files

List the important files changed.

### Tests

Which tests were added or executed.

### Results

Whether they passed or failed.

### Decisions

Any implementation decisions made.

### Risks

Anything that may require attention.

### Next Step

The next logical implementation step.

Then stop and wait for approval when the phase requires review.

---

# 36. Most Important Rules

Always remember:

1. **Read before coding.**
2. **Follow the project's documentation.**
3. **Do not invent business rules.**
4. **Do not over-engineer.**
5. **Protect tenant isolation.**
6. **Treat financial operations as transactional.**
7. **Keep Central Platform and Teacher Platform boundaries clear.**
8. **Use ASP.NET Core Web API on .NET 10.**
9. **Use PostgreSQL through Docker for development.**
10. **Write automated tests for critical behavior.**
11. **Make small, reviewable changes.**
12. **Ask before changing architectural or business decisions.**
13. **Never implement the entire project blindly.**
14. **Work in approved phases.**
15. **The human owner has final authority over architectural and business decisions.**

---

# 37. Current Instruction

Your first task is:

## PHASE 0 — PROJECT DISCOVERY

Do not write application code yet.

Inspect the repository and all available project documentation.

Then produce the **Project Discovery Report** described above.

Wait for human approval before starting Phase 1.
