namespace OnlineTeacher.Domain.Permissions;

/// <summary>
/// Catalog of platform permission codes used by the Role + Permission authorization model.
/// Permission codes are the canonical identifiers used for dynamic authorization.
/// </summary>
public static class PlatformPermissions
{
    public const string Access = "Platform.Access";
    public const string Manage = "Platform.Manage";

    /// <summary>
    /// Grants management of a platform's memberships (add member, change role, remove member).
    /// Owned by the platform's Owner role.
    /// </summary>
    public const string Membership = "Platform.Membership";

    /// <summary>Grants read access to a platform's course content structure.</summary>
    public const string CourseView = "Course.View";

    /// <summary>Grants creation, update, and deletion of a platform's courses, units, and lessons.</summary>
    public const string CourseManage = "Course.Manage";

    /// <summary>Grants read access to enrollment information for a platform's courses.</summary>
    public const string EnrollmentView = "Enrollment.View";

    /// <summary>
    /// Grants management of a platform's wallet operations: reviewing (approving/rejecting) student
    /// transfer requests and viewing the platform wallet ledger. Granted to the Owner role and to
    /// assistants who are authorized to handle wallet credits.
    /// </summary>
    public const string WalletManage = "Wallet.Manage";

    /// <summary>
    /// Grants management of student coupons: creating, listing, viewing, and revoking coupons
    /// assigned to students within the teacher platform.
    /// </summary>
    public const string CouponManage = "Coupon.Manage";

    public static readonly IReadOnlyCollection<string> All =
        new[] { Access, Manage, Membership, CourseView, CourseManage, EnrollmentView, WalletManage, CouponManage };
}