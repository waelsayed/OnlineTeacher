# Task 6 — Student Coupons (Teacher Platform Coupons)
## Discovery / Planning Draft

> **STATUS: APPROVED — PHASE 1 & PHASE 2 COMPLETE**
>
> Phase 1 (Domain) completed. Phase 2 (Infrastructure) completed.
> Phase 3 (Application + API) awaits explicit approval.

---

## Step 1 — Scope Re-verification

### Recommended Task 6 Capability

**Student Coupons (Teacher Platform Coupons)** — the ability for a teacher to create, assign, and manage discount coupons for students, and for a student to apply a coupon during course purchase to reduce the wallet debit amount.

### Why Student Coupons Is the Appropriate Next Task

1. **Natural commerce progression**: Task 5 (Wallet & Purchase) established `wallet → debit → enroll → record`. Coupons add a **discount step before the debit**, completing the payment lifecycle: `price → coupon → final amount → debit → enroll → record`.

2. **Reserved type**: `CouponCredit` (TransactionType = 3) was explicitly reserved in Task 5 for future coupon flow.

3. **Extensive documentation**: Coupons have dedicated sections in 10+ project documents: Business Rules/Policies (§10, §11, §22, §30), Domain Model (§9–12, §29), System Use Cases (UC-04, UC-24), API Design (§21, §24), Database Design (§23–24), Overview (§22), FRS (§10), Bounded Contexts (§8), NFRs (§26).

4. **Clear business rules**: AGENTS.md §12 codifies the five coupon invariants (Personal, Single-use, Expirable, Non-transferable, Non-reusable).

5. **Independent of other candidates**: Coupons don't depend on Revisions, Packages, or Exams (content types that would add parallel complexity to the purchase flow). They don't depend on Refunds (post-purchase).

6. **Dependency alignment**: Coupons extend exactly one existing service (`PurchaseCourseService`) with a clear integration point. Refunds (alternative) would require new service infrastructure.

### Evidence Summary

| Document | Section | Evidence |
|----------|---------|----------|
| AGENTS.md | §12 (Coupons) | "Personal, Assigned to one person, Single-use, Expirable, Non-transferable, Non-reusable" |
| AGENTS.md | §7 (Teacher Platform) | "Student coupons" listed as Teacher Platform responsibility |
| System Use Cases | UC-24 | "Apply Coupon for Student" — teacher creates, student uses during purchase |
| Domain Model & System Arch | §29 (Coupon Domain) | Two types: Student Coupon (teacher-created for students) and Teacher Subscription Coupon (central) |
| Domain Analysis | §10–12 | Coupon Rules (Single-Use), Discount (up to 100%), Redemption |
| API Design | §24 | Coupon flow: Exists → Not Expired → Not Consumed → Belongs to Target → Valid → Consume |
| API Design | §21 | CouponCredit listed as ledger transaction type |
| API Design | §18 | Enrollment flow includes: "Validate Payment / Coupon / Free" |
| Overview | §22 | Teacher Coupon: Code, Discount, Expiration, Applicable Product / Plan, Usage Policy, Status |
| FRS | §10 | Teacher Coupons: single-use, expiration, assigned to specific student |
| DB Design | §23–24 | Teacher Coupon table, Central Coupon table, Coupon Lifecycle |
| Domain Analysis | §11 | Discount up to **100%** explicitly approved; example: Plan Price=1000, Discount=100%, Payable=0 |
| NFRs | §26 | Coupon Integrity — concurrency protection |
| PROJECT_IMPLEMENTATION_HISTORY | §5, §11 | `CouponCredit` reserved in Task 5 |

---

## Step 2 — Complete Task 6 Plan

### 1. Task Objective

Enable teachers to create single-use discount coupons assigned to specific students, and allow students to apply those coupons during course purchase to reduce (or eliminate) the wallet debit amount, following documented business rules.

### 2. Business Problem

Without coupons, a teacher cannot offer discounts. All students pay the full course price through wallet debit. Teachers need the ability to:
- Offer partial discounts (e.g., 50% off) to specific students
- Offer free access to specific students (100% discount)
- Control coupon validity through expiration dates
- Ensure a discount can only be used once and by the intended student

### 3. Functional Scope

**In scope:**
- Teacher creates a coupon assigned to one specific student (tenant-scoped)
- Coupon properties: Code (teacher-defined), DiscountType (Percentage/Fixed), DiscountValue, ExpiresAt, Status
- Coupon lifecycle: Created → Active → Consumed / Expired
- Teacher lists coupons (with status filtering)
- Teacher views coupon details (including consumption info)
- Teacher revokes/deactivates a coupon (only if not yet consumed)
- Student applies coupon during course purchase (optional `couponCode` parameter)
- Coupon validation: not expired, not consumed, belongs to student, applicable to course
- Discount reduces the wallet debit amount: `finalAmount = max(0, coursePrice - discount)`
- `CouponCredit` FinancialTransaction recorded for the discount value
- All coupon operations within a Teacher Platform (tenant-scoped)
- New permission: `Coupon.Manage` for teacher coupon-management endpoints

### 4. Non-Goals

- Central Coupons (Teacher Subscription Coupons — separate bounded context, Central Platform)
- Coupon applicability to content other than Courses (Packages, Revisions, Exams — future Tasks)
- Bulk coupon generation
- Coupon reporting/analytics
- Coupon auto-assignment (all coupons are explicitly created by teacher)
- Refunds (Task 7)
- Auto-follow on enrollment (see §19 of API Design — documented but not yet implemented; defer decision)

### 5. Relevant Use Cases

**UC-24 — Apply Coupon for Student** (System Use Cases §25): Student/Teacher/Teacher Admin/Assistant. Teacher creates coupon for students according to permissions and policies. Discount can be percentage, fixed value, or completely free. Coupon usage is recorded and linked to the student who used it.

**UC-25 — Manage Financial Transactions** (§26): Coupon usage is recorded as part of financial operations.

**UC-26 — Teacher Financial Reports** (§27): Teacher can view coupons used in financial reports.

### 6. Relevant User Journeys

- **Teacher creates coupon**: Teacher navigates to coupon management → fills in code, discount type/value, expiration, selects student → coupon created (Active)
- **Teacher manages coupons**: Teacher lists coupons, views status, revokes unused coupons
- **Student purchases with coupon**: Student selects course → enters coupon code → system validates coupon → calculates discount → debits wallet → creates enrollment → records transactions → coupon consumed
- **Coupon expired**: Student enters expired coupon code → validation fails → purchase proceeds at full price

### 7. Domain Model

```
Teacher Platform (tenant)
    ↓
StudentCoupon (tenant-scoped)
    ├── Code (string, unique per tenant)
    ├── DiscountType (Percentage | Fixed)
    ├── DiscountValue (decimal)
    ├── ExpiresAt (DateTime)
    ├── AssignedToStudentId (Guid → Student)
    ├── Status (Active | Consumed | Expired)
    ├── ConsumedAt (DateTime?)
    ├── ConsumedInTransactionId (Guid? → FinancialTransaction)
    ├── CreatedByTeacherId (Guid → Teacher)
    └── CreatedAt (DateTime)
```

### 8. Bounded Context

- **Teacher Platform — Coupon Context** (new bounded context within Teacher Platform)
- **Teacher Platform — Financial/Purchase Context** (coupon integrates into the existing purchase flow)
- **Central Platform — Coupon Context** (out of scope for Task 6 — handles Teacher Subscription Coupons)

### 9. Entity / Value-Object Design

**New entity: `StudentCoupon`**
```
Id: Guid
TenantId: Guid (ITenantScoped)
Code: string (unique per tenant, teacher-defined)
DiscountType: DiscountType enum
DiscountValue: decimal (positive)
ExpiresAt: DateTime (UTC, must be > CreatedAt)
AssignedToStudentId: Guid (FK → Student)
Status: CouponStatus (Active, Consumed, Expired)
ConsumedAt: DateTime? (nullable, set on consumption)
ConsumedInTransactionId: Guid? (FK → FinancialTransaction)
CreatedByTeacherId: Guid
CreatedAtUtc: DateTime
UpdatedAtUtc: DateTime?
```

**New enums:**
```csharp
public enum DiscountType { Percentage, Fixed }
public enum CouponStatus { Active = 0, Consumed = 1, Expired = 2 }
```

**Value objects (optional, could be inline):**
- `CouponCode` — wraps string, validates format (non-empty, trimmed)

### 10. Database Design

**New table: `student_coupons`**
```
Column                  | Type            | Constraints
------------------------|-----------------|-----------------------------------------------
Id                      | uuid            | PK
TenantId                | uuid            | NOT NULL, FK → teacher_platforms(Id)
Code                    | varchar(100)    | NOT NULL
DiscountType            | int             | NOT NULL (0=Percentage, 1=Fixed)
DiscountValue           | decimal(18,2)   | NOT NULL, CHECK > 0
ExpiresAt               | timestamptz     | NOT NULL
Status                  | int             | NOT NULL (0=Active, 1=Consumed, 2=Expired)
AssignedToStudentId     | uuid            | NOT NULL, FK → students(Id)
ConsumedAt              | timestamptz?    | NULL
ConsumedInTransactionId | uuid?           | NULL, FK → financial_transactions(Id)
CreatedByTeacherId      | uuid            | NOT NULL
CreatedAtUtc            | timestamptz     | NOT NULL
UpdatedAtUtc            | timestamptz?    | NULL
```

**Indexes & constraints:**
- PK: `student_coupons_pkey` on (Id)
- **Unique index**: `ux_student_coupons_tenant_code` on (TenantId, Code) — enforces code uniqueness per tenant
- FK: `student_coupons_tenantid_fkey` → teacher_platforms(Id)
- FK: `student_coupons_studentid_fkey` → students(Id)
- FK: `student_coupons_transactionid_fkey` → financial_transactions(Id)
- CHECK: `ck_student_coupons_expires_at` → `ExpiresAt > CreatedAtUtc`
- CHECK: `ck_student_coupons_discount_value` → `DiscountValue > 0`
- CHECK: `ck_student_coupons_discount_type_percent` → `DiscountType = 0 AND DiscountValue <= 100 OR DiscountType = 1`
- Index: `ix_student_coupons_status` on (Status) for filtered queries

**Modifications to existing tables:**
- `financial_transactions`: The `CouponCredit` TransactionType (3) becomes active for use. Optionally add `CouponId` FK column (nullable), but this can also be tracked via the `Reference` field. Decision needed (see §13).

### 11. Application Layer

**New application services:**
1. **`CreateCouponService`** — teacher creates a coupon:
   - Validates teacher membership + `Coupon.Manage` permission
   - Validates assigned student exists (central identity)
   - Validates expiration date is in the future
   - Validates discount value (percentage 1–100, fixed > 0)
   - Generates unique code (or uses teacher-provided code)
   - Creates `StudentCoupon` with Status = Active
   - Returns coupon ID

2. **`ListCouponsService`** — teacher lists coupons:
   - Returns coupons filtered by optional status
   - Returns coupon code, discount info, student name, status, created date

3. **`GetCouponService`** — teacher views single coupon detail:
   - Includes consumption info if consumed

4. **`RevokeCouponService`** — teacher revokes/deactivates an unused coupon:
   - Validates coupon is not yet consumed
   - Sets Status = Expired

**Modified application services:**
1. **`PurchaseCourseService`** — add optional `couponCode` parameter:
   - Flow becomes:
     1. Validate course + platform (existing)
     2. If `couponCode` provided:
        - Look up coupon by code + tenant
        - Validate: not expired, not consumed, assigned to this student
        - Calculate discount → finalAmount = max(0, price - discount)
     3. Validate wallet balance >= finalAmount (modified)
     4. Debit wallet by finalAmount (modified)
     5. If coupon used: consume coupon (set status + timestamp + transaction reference)
     6. Create Enrollment (existing)
     7. Record Purchase FinancialTransaction for finalAmount (modified)
     8. If coupon used: record CouponCredit FinancialTransaction for discount amount
     9. SaveChanges (one atomic transaction)

**New repository interface:**
```csharp
public interface ICouponRepository
{
    Task<StudentCoupon?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct);
    Task<List<StudentCoupon>> ListByTenantAsync(Guid tenantId, CouponStatus? status, CancellationToken ct);
    Task<StudentCoupon?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(StudentCoupon coupon);
}
```

### 12. Infrastructure Layer

**New EF configuration:** `StudentCouponConfiguration`:
- Maps to `student_coupons` table
- Configures unique index on `(TenantId, Code)`
- Configures FKs to `students`, `teacher_platforms`, `financial_transactions`
- Configures tenant query filter (`q => q.TenantId == tenantId`)

**New migration:** Creates `student_coupons` table. Does NOT modify `financial_transactions` schema unless the `CouponId` FK decision requires it (see §13).

### 13. API Design

**Teacher endpoints** (tenant-scoped, `/{publicId}/{slug}/api/platform/coupons`):

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | /{publicId}/{slug}/api/platform/coupons | `Coupon.Manage` | Create coupon |
| GET | /{publicId}/{slug}/api/platform/coupons | `Coupon.Manage` | List coupons (?status=Active/Consumed/Expired) |
| GET | /{publicId}/{slug}/api/platform/coupons/{id} | `Coupon.Manage` | Get coupon detail |
| DELETE | /{publicId}/{slug}/api/platform/coupons/{id} | `Coupon.Manage` | Revoke coupon (only if Active) |

**Student endpoints** (modified existing):

| Method | Path | Auth | Change |
|--------|------|------|--------|
| POST | /api/student/purchase/{publicId}/{courseId} | Student JWT | Add optional `couponCode` field in request body |

**Teacher request/response DTOs:**
```csharp
// POST request
public record CreateCouponRequest(
    string Code,           // teacher-defined code
    string DiscountType,   // "Percentage" | "Fixed"
    decimal DiscountValue,
    DateTime ExpiresAt,
    Guid StudentId
);

// GET response
public record CouponResponse(
    Guid Id,
    string Code,
    string DiscountType,
    decimal DiscountValue,
    DateTime ExpiresAt,
    string Status,
    Guid StudentId,
    string? StudentName,
    DateTime? ConsumedAt,
    Guid? ConsumedInTransactionId,
    DateTime CreatedAt
);
```

**Modified student purchase request:**
```csharp
public record PurchaseRequest(
    string? CouponCode   // optional
);
```

### 14. Authorization

| Operation | Principal | Permission / Guard |
|-----------|-----------|-------------------|
| Create coupon | Teacher/Assistant | `Coupon.Manage` + tenant membership |
| List/Get coupon | Teacher/Assistant | `Coupon.Manage` + tenant membership |
| Revoke coupon | Teacher/Assistant | `Coupon.Manage` + tenant membership |
| Apply coupon during purchase | Student | Student JWT (`RequirePrincipalType("student")`) + coupon validation (AssignedToStudentId == current student) |

**New permission constant:**
```csharp
public const string CouponManage = "Coupon.Manage";
```
Auto-seeded to `Owner` role via the existing `PermissionSeeder`/`All` collection.

### 15. Tenant Isolation

- `StudentCoupon` implements `ITenantScoped` → EF global query filter protects cross-tenant reads
- Teacher endpoints require tenant membership (via `PlatformAccessGuard.RequireMemberAsync`)
- Student coupon consumption validates `coupon.AssignedToStudentId == studentId`
- Cross-tenant: Teacher A's coupons are invisible to Teacher B (tenant filter); Student A cannot consume Student B's coupon (AssignedToStudentId check)

### 16. Purchase Integration

The coupon integrates into the **existing** `PurchaseCourseService` flow. No new purchase infrastructure.

**Modified flow:**

```
1. Validate platform active                (existing)
2. Validate course published               (existing)
3. Validate course is Paid                 (existing)
4. Get/create wallet                       (existing)
5. IF couponCode provided:
   a. Look up coupon by code + tenant
   b. Validate: not expired → 422
   c. Validate: not consumed → 422
   d. Validate: assigned to this student → 422
   e. Calculate discount:
      - Percentage: discount = price × discountValue / 100
      - Fixed:      discount = min(discountValue, price)
   f. finalAmount = price - discount
   ELSE:
      finalAmount = price                     (existing)
6. Validate wallet.Balance >= finalAmount   (modified from price)
7. Debit wallet by finalAmount              (modified from price)
8. Create Enrollment                        (existing)
9. IF coupon used:
   a. Consume coupon (set status, ConsumedAt, link to transaction)
   b. Record CouponCredit FinancialTransaction (type=CouponCredit, amount=+discount, status=Completed)
10. Record Purchase FinancialTransaction    (modified: amount=-finalAmount)
11. SaveChanges (one transaction)           (existing)
```

### 17. Concurrency Strategy

**Recommendation: Pessimistic locking (`SELECT ... FOR UPDATE`)** via the existing `IUnitOfWork` / EF approach.

Rationale:
- The existing architecture uses EF Core `SaveChanges` with no explicit locking
- PostgreSQL row-level lock (`FOR UPDATE`) on the coupon row prevents double consumption
- This is simpler than an idempotency key (which requires client-generated keys and collision management)
- The lock is held for a very short duration (within the SaveChanges transaction)

Implementation approach: Add a `GetByCodeForUpdateAsync` method to `ICouponRepository` that issues:
```sql
SELECT ... FROM student_coupons WHERE tenant_id = @t AND code = @c FOR UPDATE
```

Alternative: Use an idempotency key. **Decision required** (see §14).

### 18. Transaction / Atomicity Requirements

Coupon consumption must occur in the **same database transaction** as:
- Wallet debit
- Purchase FinancialTransaction creation
- CouponCredit FinancialTransaction creation (if coupon used)
- Enrollment creation

This prevents:
- ✅ Coupon consumed but purchase failed (rollback)
- ✅ Wallet debited but coupon not consumed (rollback)
- ✅ Enrollment created without successful payment (rollback)
- ✅ Coupon reused through concurrent requests (`FOR UPDATE` lock)

The existing `IUnitOfWork.SaveChangesAsync` wrapped in the existing try/finally provides the transaction boundary. No change needed to the unit-of-work pattern.

### 19. Unit Test Plan

**Domain tests:**
- `StudentCoupon` creation (null/empty code, past expiry, zero/negative discount)
- Discount type validation: Percentage (1–100), Fixed (> 0)
- Valid status transitions: Active → Consumed, Active → Expired
- Invalid transitions: Consumed → Active, Expired → Active
- Consumption validation: wrong student, already consumed, expired
- Discount calculation:
  - 70% of 1,000 = 300 (Domain Analysis §11 example)
  - 100% of 1,000 = 0
  - Fixed 200 off 1,000 = 800
  - Fixed 1,500 off 1,000 = 0 (capped)
- `CouponCode` value-object validation

**Application service tests:**
- `CreateCouponService` — valid creation, duplicate code, invalid student, past expiry
- `ListCouponsService` — status filtering, tenant isolation
- `RevokeCouponService` — revoke active, revoke consumed (422)

### 20. Integration Test Plan

**Happy paths:**
- Full flow: create coupon (teacher) → purchase with coupon (student) → verify debit reduced → verify CouponCredit + Purchase transactions → verify coupon status = Consumed
- 100% discount: purchase with full discount → zero wallet debit → enrollment created → CouponCredit recorded
- Fixed discount: purchase with fixed discount → correct debit reduction

**Validation failures:**
- Expired coupon → 422, no purchase, coupon status unchanged
- Already-consumed coupon → 422
- Wrong student coupon → 403/422
- Invalid coupon code → 404/422
- Insufficient wallet balance after discount → 422, coupon NOT consumed (verify rollback)
- Free course + coupon → 422 (free courses use direct-enroll)

**Concurrency:**
- Two simultaneous attempts to consume the same coupon → one succeeds, the other fails (no double consumption)

**Authorization:**
- Cross-tenant: Teacher A cannot view Teacher B's coupons → 404
- Assistant without `Coupon.Manage` → 403
- Anonymous → 401

**Regression:**
- Normal paid purchase without coupon still works
- Free course enrollment unchanged
- Transfer/wallet/approve flows unchanged
- Enrollment cancellation/re-enrollment unchanged
- No auto-follow introduced

### 21. Documentation Changes

- `IMPLEMENTATION_PLAN.md` — add Task 6 section under Implementation Status
- `PROJECT_IMPLEMENTATION_HISTORY.md` — add Task 6 section after Task 5
- `Tasks/TASK6-DRAFT.md` — finalize with approved decisions

### 22. Expected Files / Projects Affected

**Domain project:**
- `src/OnlineTeacher.Domain/Entities/StudentCoupon.cs` (new)
- `src/OnlineTeacher.Domain/Enums/DiscountType.cs` (new)
- `src/OnlineTeacher.Domain/Enums/CouponStatus.cs` (new)
- `src/OnlineTeacher.Domain/Permissions/PlatformPermissions.cs` (modified — add `CouponManage` constant)
- (Optionally) `src/OnlineTeacher.Domain/ValueObjects/CouponCode.cs` (if extracted as value object)

**Application project:**
- `src/OnlineTeacher.Application/Persistence/ICouponRepository.cs` (new)
- `src/OnlineTeacher.Application/Services/CreateCouponService.cs` (new)
- `src/OnlineTeacher.Application/Services/ListCouponsService.cs` (new)
- `src/OnlineTeacher.Application/Services/GetCouponService.cs` (new)
- `src/OnlineTeacher.Application/Services/RevokeCouponService.cs` (new)
- `src/OnlineTeacher.Application/Services/PurchaseCourseService.cs` (modified — add coupon parameter + discount logic)

**Infrastructure project:**
- `src/OnlineTeacher.Infrastructure/Persistence/Configurations/StudentCouponConfiguration.cs` (new)
- `src/OnlineTeacher.Infrastructure/Persistence/Repositories/CouponRepository.cs` (new)
- `src/OnlineTeacher.Infrastructure/Persistence/OnlineTeacherDbContext.cs` (modified — add `DbSet<StudentCoupon>`)
- Migration: `YYYYMMDDHHMMSS_AddStudentCoupons.cs` (new)

**API project:**
- `src/OnlineTeacher.Api/Controllers/PlatformCouponController.cs` (new — teacher coupon endpoints)
- `src/OnlineTeacher.Api/Contracts/CouponContracts.cs` (new — request/response DTOs)
- `src/OnlineTeacher.Api/Contracts/PurchaseRequest.cs` (new — or inline couponCode in existing controller)
- `src/OnlineTeacher.Api/Controllers/StudentController.cs` (modified — add couponCode to purchase endpoint)
- `src/OnlineTeacher.Api/Program.cs` (modified — register new services)

**Test projects:**
- Unit tests for domain entities + value objects
- Application service tests for coupon services
- Integration tests for coupon + purchase flow

### 23. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Coupon code collision (teacher-defined) | Duplicate codes within tenant | DB unique index on (TenantId, Code) |
| Concurrency race on consumption | Double spend of single coupon | `FOR UPDATE` pessimistic lock |
| Atomicity failure (coupon consumed, purchase fails) | Student loses coupon | Same transaction rollback |
| Percentage discount precision | Rounding errors in financial amounts | Use decimal type; round to 2 decimal places |
| Coupon + Refund interaction (future Task 7) | Restoring consumed coupons on refund | Document that Task 6 does NOT handle this |
| Auto-follow integration (API Design §19) | Potential scope creep | Surface as decision; defer if not critical |

### 24. Edge Cases

1. **100% discount → zero wallet debit**: The wallet is **not** debited (`Debit(0)` rejected by domain invariant requiring positive amount). The purchase creates Enrollment and records CouponCredit + Purchase (amount=0) transactions without a wallet debit. **Decision needed** (see §9).

2. **Fixed discount > course price**: Cap at `finalAmount = max(0, price - discount)`.

3. **Coupon expires between code entry and purchase submission**: Validate expiration at the point of purchase, not when code is entered (if there's a separate validation step).

4. **Student attempts to use consumed coupon code**: Reject with 422 "Coupon has already been consumed."

5. **Teacher revokes coupon while student is mid-purchase**: The row lock will ensure the coupon is either Active (purchase proceeds and consumes it) or consumed/expired (purchase fails with coupon validation error).

6. **Duplicate purchase after coupon consumption**: The existing active-enrollment duplicate check prevents buying the same course twice. The consumed coupon is already terminal.

7. **Coupon code with special characters / leading/trailing whitespace**: Normalize (trim, uppercase) on creation and lookup.

8. **Multiple coupons on same purchase**: Not supported. Only one coupon per purchase. The existing `couponCode` parameter accepts a single code.

### 25. Definition of Done

- [ ] Domain: `StudentCoupon` entity + `DiscountType`/`CouponStatus` enums + validation invariants
- [ ] Domain: Discount calculation logic (percentage + fixed, cap at zero)
- [ ] Domain: Comprehensive unit tests for all business rules
- [ ] Permission: `Coupon.Manage` added to `PlatformPermissions.All`
- [ ] Infrastructure: `StudentCouponConfiguration` EF config + DB unique index + FKs + check constraints
- [ ] Infrastructure: `CouponRepository` with `GetByCodeAsync`, `ListByTenantAsync`, `GetByIdAsync`, `Add`, and `GetByCodeForUpdateAsync` for concurrency
- [ ] Infrastructure: Migration creating `student_coupons` table
- [ ] Application: `CreateCouponService`, `ListCouponsService`, `GetCouponService`, `RevokeCouponService`
- [ ] Application: Modified `PurchaseCourseService` — optional `couponCode` parameter + discount logic
- [ ] Application: Coupon consumption integrated in same atomic transaction as purchase (wallet debit + enrollment + transactions)
- [ ] Application: Concurrency protection via `FOR UPDATE` or idempotency key
- [ ] API: Teacher coupon endpoints (`POST/GET/DELETE /coupons`) with `Coupon.Manage`
- [ ] API: Modified student purchase endpoint — optional `couponCode` field
- [ ] Authorization: Tenant filter on `StudentCoupon`; membership guard on teacher endpoints; `AssignedToStudentId` validation on consumption
- [ ] Integration tests: Full purchase-with-coupon flow, expired/wrong-student/consumed rejection, insufficient-balance rollback, concurrency, cross-tenant isolation
- [ ] Regression tests: Normal purchase without coupon, free enrollment, wallet/transfer flows unchanged
- [ ] `dotnet build --warnaserror` — 0 warnings, 0 errors
- [ ] Unit: existing + new — all pass
- [ ] Integration: existing + new — all pass
- [ ] Documentation updated (`IMPLEMENTATION_PLAN.md`, `PROJECT_IMPLEMENTATION_HISTORY.md`)

---

## Step 3 — Explicit Business Decisions Requiring Approval

### Decision 1: Coupon ownership
**Documented:** System Use Cases UC-24 — teacher creates coupon for students. Domain Model §29 — Student Coupon "assigned to a specific student." Tenant-scoped.
**Recommendation:** Each `StudentCoupon` is:
- ✅ Assigned to exactly one student
- ✅ Tenant-scoped (belongs to Teacher Platform)
- ✅ Created/managed by Teacher Platform staff (teacher/assistant with `Coupon.Manage`)
**Status: REQUIRES APPROVAL**

### Decision 2: Coupon discount type
**Documented:** Domain Analysis §11 — discount can be "percentage or specific value or completely free." Overview §22 — "percentage or financial value or full 100% discount." System Use Cases UC-24 — "discount can be a percentage, a specific value, or completely free."
**Recommendation:** Support **both** `Percentage` and `Fixed` discount types via a `DiscountType` enum (0 = Percentage, 1 = Fixed).
- Percentage: 1–100, applied as `price × value / 100`
- Fixed: positive decimal, applied as `price - value` (capped at zero)
**Status: REQUIRES APPROVAL**

### Decision 3: Discount limits (100%)
**Documented:** Domain Analysis §11 — "Discount value can be any percentage the system allows, including **100%**." Example: Plan Price = 1,000 EGP, Discount = 100%, Payable Amount = 0 EGP. AGENTS.md §12 mentions 100% is possible.
**Recommendation:** Allow up to 100% for percentage discounts. No upper limit on fixed discounts (the `max(0, price - discount)` cap prevents negative pricing).
**Status: REQUIRES APPROVAL**

### Decision 4: Coupon expiration
**Documented:** FRS §10 — "expiration date." Domain Model §29 — coupon states include "Expired." Overview §22 — Teacher Coupon must have "Expiration." AGENTS.md §12 — "Expirable."
**Recommendation:**
- Every coupon has a required `ExpiresAt` (DateTime, UTC)
- Expiration is checked at the moment of purchase (not at code entry)
- An expired coupon CANNOT be reactivated (terminal state)
- Expiration is enforced in application code + DB CHECK constraint (`ExpiresAt > CreatedAtUtc`)
**Status: REQUIRES APPROVAL**

### Decision 5: Course applicability
**Documented:** Overview §22 — Teacher Coupon should have "Applicable Product / Plan." API Design §24 — "Valid for Current Plan / Value." This is ambiguous: does "Product / Plan" mean Course? Or multiple content types?
**Recommendation (simplest):** Start with **course-level applicability only**. A coupon is valid for any Paid Course within the Teacher Platform (no per-course restriction). This avoids coupling coupons to specific courses or content types. Extend to specific products/plans in a future Task.
**Alternative A:** Coupon applies to one specific Course (add `CourseId` FK). More restrictive.
**Alternative B:** Coupon applies to all eligible Courses in the Teacher Platform (current recommendation).
**Status: REQUIRES APPROVAL**

### Decision 6: Single-use rule
**Documented:** AGENTS.md §12 — "Single-use." Domain Analysis §10 — "Single-Use Coupon, i.e., it can be used only once." FRS §10 — "single-use."
**Recommendation:** Once consumed, the coupon is **permanently terminal**. No reactivation. Consumption sets Status = Consumed + ConsumedAt timestamp. Enforced by: (a) application validation, (b) unique consumption (row lock), (c) DB CHECK preventing consumed → active transition.
**Status: REQUIRES APPROVAL**

### Decision 7: Student restriction
**Documented:** AGENTS.md §12 — "Personal, Assigned to one person." Domain Model §29 — "Cannot be reused by another student." API Design §24 — "Belongs to Current Target."
**Recommendation:** If Student B attempts to use Student A's coupon → 422 BusinessRuleViolation ("This coupon is assigned to a different student"). Enforced by comparing `coupon.AssignedToStudentId` with the authenticated student's ID.
**Status: REQUIRES APPROVAL**

### Decision 8: Purchase interaction — Price → Coupon → Final Amount → Wallet Debit → Enrollment
**Documented:** API Design §24 (Coupon Application flow) + §22 (Purchase Transaction flow). The combined flow is documented in API Design §18: "Validate Payment / Coupon / Free."
**Recommendation:** The exact flow:
```
coursePrice → validate coupon → calculate discount → finalAmount = max(0, price - discount)
→ validate wallet.Balance >= finalAmount → debit wallet by finalAmount
→ consume coupon → record CouponCredit (+discount) transaction → record Purchase (-finalAmount) transaction
→ create Enrollment
```

**Examples:**
- Price=1000, Discount=70% → finalAmount=300, debit=300, CouponCredit=+700
- Price=1000, Discount=100% → finalAmount=0, debit=0 (no debit), CouponCredit=+1000, Purchase=0
- Price=1000, Fixed=200 → finalAmount=800, debit=800, CouponCredit=+200
- Price=1000, Fixed=1500 → finalAmount=0, debit=0, CouponCredit=+1000, Purchase=0
- Balance=500, Price=1000, Discount=70% → finalAmount=300, sufficient, success
- Balance=200, Price=1000, Discount=70% → finalAmount=300, insufficient → 422, coupon NOT consumed
**Status: REQUIRES APPROVAL**

### Decision 9: Zero-value purchase (100% discount)
**Documented:** Domain Analysis §11 — Example with 100% discount: "Payable Amount = 0 EGP. In this case the teacher gets the subscription for free." Applied to student coupons: student gets the course for free.
**Approved decision (TASK6-1.md §14):**
- Do NOT call `Wallet.Debit(0)`.
- Create the Enrollment.
- Record `CouponCredit` FinancialTransaction for the discount value (type=CouponCredit, amount=+price).
- Do NOT create a Purchase transaction with amount 0.
- The wallet balance remains unchanged.

**Alternative A:** Record a Purchase transaction with amount=0 (rejected).

### Decision 10: Coupon consumption atomicity
**Documented:** API Design §22 — "Purchase Transaction must be as Atomic as possible." NFR §26 — concurrency protection.
**Recommendation:** Coupon consumption MUST occur in the SAME atomic operation as wallet debit, FinancialTransaction creation, and Enrollment creation, via the existing `IUnitOfWork.SaveChangesAsync` transaction boundary.
**Status: REQUIRES APPROVAL**

### Decision 11: Concurrency strategy
**Documented:** NFR §26 — Coupon Integrity requires concurrency protection. API Design §22 — atomicity.
**Recommendation:** Use **PostgreSQL row-level locking (`SELECT ... FOR UPDATE`)** via a dedicated `GetByCodeForUpdateAsync` repository method. This is simpler than an idempotency key approach because:
- No client-generated idempotency key required
- No collision/retry logic needed
- Lock is held for the duration of the SaveChanges transaction (milliseconds)
- Reuses the existing transaction infrastructure

**Alternative:** Idempotency key on purchase request (client sends `Idempotency-Key` header, system deduplicates). More robust for network retries but requires idempotency storage and key management.

**Recommendation:** Use `FOR UPDATE` locking.
**Status: REQUIRES APPROVAL**

### Decision 12: Coupon.Manage permission scope
**Documented:** The existing permission model (dynamic permissions, `PlatformPermissions` static class). The project pattern is one permission per business capability.
**Recommendation:** Add a single `Coupon.Manage` permission constant covering: create, list, get detail, revoke. No separate read/manage split (unnecessary granularity — unlike `Course.View`/`Course.Manage` which has distinct use cases). Auto-seeded to Owner role.
**Status: REQUIRES APPROVAL**

### Decision 13: CouponCredit TransactionType semantics
**Documented:** TransactionType enum already has `CouponCredit = 3` reserved. API Design §21 lists "Coupon Credit" as a ledger transaction.
**Approved decision (TASK6-1.md §13):**
- CouponCredit is **informational/audit only**. It MUST NOT change the student's wallet balance.
- Recorded as a separate FinancialTransaction alongside the purchase flow.
- `CouponCredit`: type=CouponCredit, amount=+discount, status=Completed — does not credit the wallet.
- `Purchase`: type=Purchase, amount=-finalAmount, status=Completed — this is the actual wallet debit record.
- For 100% discount: only CouponCredit is recorded (no Purchase transaction with amount 0).

### Decision 14: ConsumedInTransactionId reference
**Approved decision (TASK6-1.md §15):**
- ConsumedInTransactionId should reference the **Purchase FinancialTransaction** when a paid purchase exists (finalAmount > 0).
- For 100% discount (no Purchase transaction), ConsumedInTransactionId references the CouponCredit FinancialTransaction.

### Decision 14: Refund interaction (Task 6 boundary)
**Documented:** Task 5 reserved `Refund` TransactionType. Task 7 is expected to address refunds.
**Recommendation:** Task 6 does NOT implement refund behavior. If a purchase used a coupon and is later refunded (in Task 7), the question of whether the consumed coupon should be restored is deferred. For Task 6, consumed coupons remain consumed.
**Status: REQUIRES APPROVAL**

### (Optional) Decision 15: Auto-follow on enrollment/purchase
**Documented:** API Design §19 — "When creating Course Enrollment: if the Student does not follow the Teacher, a Follow is created. If the Student is enrolled, they are not allowed to cancel the Follow resulting from the Enrollment as long as the Enrollment exists." This was NOT implemented in Tasks 1–4 (documented non-goal).
**Recommendation:** Do NOT implement auto-follow in Task 6. It is a behavior change that crosses multiple existing tests and should be implemented as a separate task or as part of a Task 4 revision. Task 6 should focus on coupons only.
**Status: REQUIRES APPROVAL**

---

## Step 4 — Architecture Review

The Task 6 plan extends the existing Task 5 architecture without creating a parallel purchasing system:

| Existing Component | How Task 6 Extends It |
|-------------------|----------------------|
| `PurchaseCourseService` | Add optional `couponCode` parameter; modify validation and debit logic |
| `StudentWallet.Debit(amount)` | Debit `finalAmount` instead of `price` (no structural change) |
| `FinancialTransaction` | New `CouponCredit` type (already reserved); no schema change if `Reference` field is used for coupon link |
| `IUnitOfWork` / `SaveChangesAsync` | Same atomic transaction boundary; no change |
| `ITenantScoped` / tenant filter | `StudentCoupon` implements the same interface; same protection |
| `PlatformPermissions` | Add one new permission constant (same pattern) |
| `PlatformAccessGuard` | Same membership guard for teacher endpoints |
| `RequirePrincipalType("student")` | Same student JWT guard for purchase |
| `EfUnitOfWork.Translate` | May need a new `DbUpdateException` mapping for coupon unique constraint |

**No new architectural patterns introduced.** No CQRS, no event bus, no new abstractions.

---

## Step 5 — Migration / Database Impact

**New objects created by Task 6:**

| Object | Type | Details |
|--------|------|---------|
| `student_coupons` table | New | See 12-column schema in §10 |
| `ux_student_coupons_tenant_code` | Unique index | `(TenantId, Code)` |
| `ck_student_coupons_expires_at` | CHECK | `ExpiresAt > CreatedAtUtc` |
| `ck_student_coupons_discount_value` | CHECK | `DiscountValue > 0` with discount-type validation |
| `ix_student_coupons_status` | Index | For status-filtered queries |
| FK: AssignedToStudentId → students | FK | Restrict (no cascade) |
| FK: TenantId → teacher_platforms | FK | Existing pattern |
| FK: ConsumedInTransactionId → financial_transactions | FK | Nullable; Restrict (no cascade) |

**Existing objects potentially modified:**
- `financial_transactions`: If we add `CouponId` FK (nullable) — **Decision 13 affects this**

**Migration file:** One migration: `YYYYMMDDHHMMSS_AddStudentCoupons.cs` — creates the `student_coupons` table with all indexes, FKs, and constraints.

---

## Step 6 — Test Strategy

### Unit tests (new)

| Layer | Count (est.) | What |
|-------|-------------|------|
| Domain | 10–15 | Creation validation, status transitions, discount calculation, edge cases |
| Application | 15–20 | Create/List/Get/Revoke services + PurchaseCourseService with coupon |

### Integration tests (new)

| Scenario | What it validates |
|----------|-------------------|
| Happy: purchase with percentage coupon | Full flow success |
| Happy: purchase with fixed coupon | Full flow success |
| Happy: purchase with 100% discount | Zero wallet debit, enrollment created, transactions recorded |
| Validation: expired coupon | 422, coupon not consumed |
| Validation: already-consumed coupon | 422 |
| Validation: wrong-student coupon | 422 |
| Validation: invalid code | 404 |
| Validation: insufficient balance after discount | 422, coupon NOT consumed (rollback verified) |
| Concurrency: two simultaneous attempts | Exactly one succeeds |
| Auth: cross-tenant coupon view | 404 |
| Auth: assistant without Coupon.Manage | 403 |
| Auth: anonymous purchase with coupon | 401 |
| Regression: purchase without coupon | Works as before (no regression) |
| Regression: free course enrollment | Unchanged |
| Regression: wallet transfer/approve flow | Unchanged |

### Estimated new test count: ~30 unit + ~15 integration

---

## Files Inspected

- `PROJECT_DOCUMENTATION/System Use Cases & User Journeys.html` (UC-24)
- `PROJECT_DOCUMENTATION/Domain Analysis & Domain Model.html` (§9–12 coupons)
- `PROJECT_DOCUMENTATION/Domain Model & System Architecture.html` (§26 Refund, §29 Coupon)
- `PROJECT_DOCUMENTATION/Overview & System Architecture.html` (§22 Coupon Domain)
- `PROJECT_DOCUMENTATION/API Design & Application Layer Architecture.html` (§18–24)
- `PROJECT_DOCUMENTATION/Database Design & Data Architecture.html` (§23–24)
- `PROJECT_DOCUMENTATION/Functional Requirements Document.html` (§10 Coupons)
- `PROJECT_DOCUMENTATION/Bounded Contexts & Domain Architecture.html` (§8)
- `PROJECT_DOCUMENTATION/Non-Functional Requirements (NFR).html` (§26 Coupon Integrity)
- `PROJECT_IMPLEMENTATION_HISTORY.md` (§5, §11, §15)
- `IMPLEMENTATION_PLAN.md` (§24 Implementation Status)
- `AGENTS.md` (§12 Coupons)
- `Tasks/TASK5-REVIEW.md`, `Tasks/TASK6.md`
- `src/OnlineTeacher.Application/Services/PurchaseCourseService.cs`
- `src/OnlineTeacher.Domain/Entities/StudentWallet.cs`, `FinancialTransaction.cs`
- `src/OnlineTeacher.Domain/Enums/TransactionType.cs`, `FinancialTransactionStatus.cs`
- `src/OnlineTeacher.Domain/Permissions/PlatformPermissions.cs`
- `src/OnlineTeacher.Api/Controllers/StudentController.cs`

---

## Task 6 Plan Summary

| Aspect | Summary |
|--------|---------|
| **Capability** | Student Coupons (Teacher Platform Coupons) |
| **Evidence** | 10+ project documents, UC-24, reserved `CouponCredit` type |
| **New entity** | `StudentCoupon` (tenant-scoped) |
| **New permission** | `Coupon.Manage` |
| **Services modified** | `PurchaseCourseService` — optional `couponCode` parameter |
| **Services added** | `CreateCouponService`, `ListCouponsService`, `GetCouponService`, `RevokeCouponService` |
| **DB changes** | New `student_coupons` table (1 migration) |
| **API changes** | Teacher coupon endpoints + modified student purchase endpoint |
| **Concurrency** | `SELECT ... FOR UPDATE` row-level locking |
| **Atomicity** | Coupon consumption within same transaction as purchase |
| **Tests** | ~30 unit + ~15 integration |
| **Business decisions** | 15 decisions requiring approval (see §3 above) |

---

## Approved Decisions (from TASK6-1.md)

All 15 business decisions from the planning draft have been reviewed and approved with these final specifications:

| # | Decision | Approved Result | Notes |
|---|----------|----------------|-------|
| 1 | Coupon ownership | One student, tenant-scoped, teacher-managed | — |
| 2 | Discount type | Both Percentage and Fixed via `DiscountType` enum | — |
| 3 | Discount limits | Percentage 1–100% incl. 100%; Fixed positive, capped at zero | — |
| 4 | Expiration | Required, terminal, enforced at purchase | — |
| 5 | Course applicability | All Paid Courses in Teacher Platform (no CourseId on coupon) | — |
| 6 | Single-use | Permanently terminal after consumption | — |
| 7 | Student restriction | Wrong-student → 422 | — |
| 8 | Purchase flow | Price → Coupon → FinalAmount → Validate Wallet → Debit → Consume → Enroll → Record | — |
| 9 | Zero-value (100%) | No `Wallet.Debit(0)`, create Enrollment, record CouponCredit only, no Purchase tx | Critical: no zero-amount Purchase tx |
| 10 | Atomicity | Coupon in same transaction as purchase | — |
| 11 | Concurrency | `SELECT ... FOR UPDATE` row-level lock | — |
| 12 | Permission scope | Single `Coupon.Manage` for CRUD + revoke | — |
| 13 | CouponCredit semantics | Informational/audit only; does NOT change wallet balance | No wallet credit |
| 14 | ConsumedInTransactionId | References Purchase FinancialTransaction (paid) or CouponCredit tx (100% free) | — |
| 15 | Refund boundary | Task 6 does not handle refunds | Deferred to Task 7 |
| 16 | Auto-follow | NOT implemented in Task 6 | Separate concern |

---

**TASK 6 STATUS: APPROVED — PHASE 1 (DOMAIN) IN PROGRESS**