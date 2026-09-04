Task 6 — Student Coupons

The Task 6 planning draft has been reviewed and approved with the following final decisions:

1. StudentCoupon is assigned to exactly one student and is tenant-scoped.
2. Support both Percentage and Fixed discounts.
3. Percentage discount: 1–100%, including 100%.
4. Fixed discount: positive value, capped so final amount never goes below zero.
5. Every coupon has a required expiration date and cannot be reactivated after expiration/revocation/consumption.
6. Every StudentCoupon is tied to exactly one specific Course. CourseId is stored on the coupon and the coupon is only valid for that Course. (The earlier "No CourseId / applies to all Paid Courses" decision is superseded.)
7. Coupon is single-use and permanently terminal after consumption.
8. A coupon can only be used by its assigned student.
9. Purchase flow is:
   Course Price → Validate Coupon → Calculate Final Amount → Validate Wallet → Debit Final Amount → Consume Coupon → Create Enrollment → Record Financial Transactions.
10. Coupon consumption, wallet debit, enrollment, and financial transactions must be atomic in the same database transaction.
11. Use PostgreSQL SELECT ... FOR UPDATE to prevent concurrent double consumption.
12. Add one permission: Coupon.Manage.
13. CouponCredit is informational/audit only. It MUST NOT change the student's wallet balance.
14. For a 100% discount, final amount is zero:

* Do NOT call Wallet.Debit(0).
* Create the Enrollment.
* Record CouponCredit for the discount value.
* Do NOT create a Purchase transaction with amount 0.

15. ConsumedInTransactionId should reference the Purchase FinancialTransaction when a paid purchase exists.
16. Refund behavior is outside Task 6 and remains for Task 7.
17. Auto-follow is NOT part of Task 6.

Implementation rules:

* Read Tasks/TASK6-DRAFT.md and the existing project documentation/code before starting.
* Update TASK6-DRAFT.md first so it reflects these approved decisions.
* Do NOT invent new business rules.
* Follow the architecture and patterns already established in Tasks 1–5.
* Maintain tenant isolation, PrincipalType authorization, PlatformAccessGuard, dynamic permissions, and existing service/repository patterns.
* Do not introduce CQRS, event bus, or parallel purchasing infrastructure.
* Do not push anything to origin.
* Do not start later phases automatically.

PHASE 1 ONLY — DOMAIN

Implement only the Domain layer for Task 6:

* StudentCoupon entity
* DiscountType enum
* CouponStatus enum
* Required domain validation/invariants
* Discount calculation logic
* Consumption/revocation/expiration state transitions as defined above
* Coupon code normalization/validation if appropriate
* Add Coupon.Manage to the existing PlatformPermissions catalog only if this belongs to the established Domain permission pattern.

Add focused domain unit tests following the existing project testing conventions.

Do NOT implement:

* EF configuration
* repositories
* migrations
* application services
* API endpoints
* PurchaseCourseService integration
* integration tests
* documentation beyond updating TASK6-DRAFT.md with the approved decisions

Run:

* dotnet build --warnaserror
* Relevant domain/unit tests

Create ONE logical commit for Phase 1 only, following the existing commit naming convention.

Then STOP and report:

* files changed
* domain decisions implemented
* tests passed
* build result
* commit hash

Do not proceed to Phase 2 until I explicitly approve it.
