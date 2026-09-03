# Task 3 — Planning Only / No Implementation

Task 2 has been officially reviewed and APPROVED.

Current approved state:

* Steps 0–8: complete and approved.
* Task 1 — Teacher Platform Management: complete and approved.
* Task 2 — Central Student Identity & Following: complete and approved.
* Task 2 latest commits:

  * `3c36dff`
  * `10c2a81`
  * `8780be2`
  * `509962e`
  * `9bd8163`
  * `fa6d7e7`
* Working tree is clean.
* Repository is ahead of origin; NOTHING should be pushed.
* Full current test baseline is green:

  * Unit: 221/221
  * Integration: 39/39
  * Build with `--warnaserror`: 0 warnings / 0 errors.

## Your task now

Do NOT implement Task 3 yet.

First, inspect:

1. `AGENTS.md`
2. `IMPLEMENTATION_PLAN.md`
3. All approved Task 1 and Task 2 documentation.
4. Current Domain/Application/Infrastructure/API implementation.
5. Existing tests and established conventions.

Then determine the next logical Task 3 according to the approved project architecture and implementation plan.

Produce a detailed **Task 3 Implementation Plan only**.

## The plan MUST contain

### 1. Task 3 objective

Clearly state:

* What business capability Task 3 introduces.
* Why it belongs at this point in the implementation sequence.
* Which bounded context it belongs to.
* Whether it is Central Platform, Teacher Platform, or shared infrastructure.

### 2. Scope

List exactly what will be implemented.

Separate:

* Domain
* Persistence / Infrastructure
* Application
* Authentication / Authorization
* API
* Tests
* Documentation

### 3. Business rules

List every business rule that must be enforced.

Do not invent rules merely because they seem useful.

Explicitly identify:

* Rules already approved in previous tasks.
* New rules requiring human approval.
* Rules that should remain out of scope.

### 4. Data model

Describe:

* New entities/value objects if any.
* Relationships.
* Tenant scope.
* Required indexes/unique constraints.
* Foreign keys.
* Audit requirements.
* Whether existing entities change.

Do NOT introduce a new PublicId convention unless explicitly justified.

### 5. Authorization and tenant isolation

Explain exactly:

* Which endpoints are public.
* Which require Teacher authentication.
* Which require Student authentication.
* Which require platform membership.
* Which require specific permissions.
* Whether the operation is central or tenant-scoped.
* How the existing Tenant Resolution → Authorization architecture is preserved.

Do not introduce global JWT tenant binding.

### 6. API contract

For every proposed endpoint specify:

* HTTP method
* Route
* Authentication/principal type
* Required permission/role if applicable
* Request DTO
* Response DTO
* Important status/error behavior

Follow existing API conventions.

### 7. Application services and ports

Propose the smallest necessary services/repositories/interfaces.

Do NOT introduce:

* CQRS/MediatR
* Generic repositories
* Event bus
* Unnecessary abstractions
* Unnecessary background processing

Follow the existing one-service-per-use-case style.

### 8. Tests

Specify:

* Domain tests
* Application/service tests
* Authorization tests
* Integration tests
* Tenant-isolation tests
* Negative/error cases

Preserve all existing tests.

### 9. Migration / database impact

Identify all schema changes and constraints.

### 10. Explicit non-goals

Clearly list functionality that Task 3 must NOT implement.

Do not silently pull future tasks into Task 3.

### 11. Risks / ambiguities

Identify anything that requires a product/business decision before implementation.

For every ambiguity, provide:

* The question.
* Recommended option.
* Why.

Do not resolve a business-rule ambiguity by assumption.

### 12. Expected commits

Suggest small logical commits by layer/phase, consistent with previous tasks.

## Critical instruction

This is a PLANNING PHASE ONLY.

Do NOT:

* modify source code
* create migrations
* modify database schema
* modify tests
* modify API contracts
* modify documentation except optionally creating/updating a planning document if that is already the established workflow
* commit
* push

If the existing `IMPLEMENTATION_PLAN.md` already defines Task 3 clearly, use it as the primary source of truth and reconcile it with the actual codebase.

If the plan conflicts with the implemented architecture or approved Task 1/Task 2 decisions, STOP and explicitly report the conflict instead of silently choosing.

At the end, report:

1. Proposed Task 3 title
2. Objective
3. Scope
4. Business rules
5. Data model
6. API
7. Authorization/tenant model
8. Tests
9. Risks/ambiguities requiring approval
10. Proposed commit sequence

Then STOP and wait for human review.

Do not start implementation until explicit approval is given.
