Phase 1 approved.

Proceed with **Task 6 — Phase 2: Infrastructure only**.

Implement only:

* StudentCoupon EF configuration
* DbSet registration
* ICouponRepository implementation
* PostgreSQL persistence
* Required indexes, unique constraint, FK constraints, and CHECK constraints
* Tenant query filter
* Migration for `student_coupons`

Important:

* Follow the existing Task 1–5 EF/repository/migration patterns exactly.
* Preserve tenant isolation.
* `(TenantId, Code)` must be unique.
* Every StudentCoupon is tied to exactly one specific Course. `CourseId` on StudentCoupon references the specific Course and is required. (The earlier "No CourseId / applies to all Paid Courses" decision is superseded.)
* `ConsumedInTransactionId` should reference the Purchase FinancialTransaction as approved.
* Do not modify unrelated existing database relationships.
* Do not introduce schema changes that are not required by the approved Task 6 plan.
* Do not implement Application, API, or integration tests yet.
* Do not proceed to Phase 3.
* Do not push to origin.

Run:

* `dotnet build --warnaserror`
* Relevant infrastructure tests, if applicable.

Create ONE logical commit for Phase 2 only.

Then STOP and report:

* files changed
* migration/database changes
* constraints/indexes/FKs added
* tests/build results
* commit hash

Wait for my explicit approval before Phase 3.
