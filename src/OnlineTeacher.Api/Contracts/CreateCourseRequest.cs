namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /{publicId}/{slug}/api/platform/courses</c>.
/// A course is always created in Draft status; units are added separately. Pricing is optional:
/// a course is Free by default. To create a Paid course, supply <c>Paid</c> as the pricing type
/// plus a positive price in EGP.
/// </summary>
public sealed record CreateCourseRequest(
    string? Title,
    string? Summary,
    string? PricingType = null,
    decimal? Price = null);
