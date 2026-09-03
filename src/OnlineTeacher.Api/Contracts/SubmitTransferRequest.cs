namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Submitted by a student to request a wallet credit Transfer Request within a Teacher Platform.
/// Amount is in EGP. Payment method is a supported external mobile-payment channel; the transfer
/// reference is optional free-text metadata. Validation is enforced by the application service and
/// the controller.
/// </summary>
public sealed record SubmitTransferRequest(
    decimal Amount,
    string PaymentMethod,
    string? TransferReference);
