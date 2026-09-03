Approved — finalize with **Option 1: domain-only uniqueness/order invariants**.

The removal of DB unique indexes on `(CourseId, Position)` and `(UnitId, Position)` is an intentional architectural decision, not a defect.

Finalize the implementation with these requirements:

* The Course/Unit aggregate is the single writer for ordering.
* Domain logic guarantees unique and contiguous positions.
* No application path may bypass the aggregate and directly manipulate Position.
* Reordering must remain atomic through the existing UnitOfWork/transaction.
* Keep the domain tests covering uniqueness, contiguity, and reorder behavior.
* Keep the integration coverage for course/unit/lesson ordering and tenant isolation.
* Do NOT introduce deferrable PostgreSQL constraints, hand-managed migration SQL, or EF snapshot hacks.
* Do NOT add a replacement abstraction just to compensate for the removed DB constraint.

Document clearly in `TASK3.md` / implementation documentation:

> DB-level unique constraints on CourseId+Position and UnitId+Position were intentionally not used because EF Core's change-tracking/topological ordering conflicts with atomic reordering when those unique indexes are modeled as immediate uniqueness constraints. Ordering invariants are therefore enforced by the Course/Unit domain aggregates, which are the single writers of ordering state.

Also make clear that this is a deliberate deviation from the original Task 3 planning wording.

Before finalizing:

* `dotnet build --warnaserror`
* Full Unit tests
* Full Integration tests
* Verify migration
* Verify reorder behavior
* Verify tenant isolation
* Verify authorization
* Verify no existing Task 1/Task 2 regressions
* Verify git status

Current reported results of 281/281 Unit and 51/51 Integration are accepted, assuming the above architectural conditions are satisfied.

Do not push.

After final verification, report the final commit(s), exact test results, migration status, documentation changes, and git status, then STOP for human review.
