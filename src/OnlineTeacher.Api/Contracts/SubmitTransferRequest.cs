using System.ComponentModel.DataAnnotations;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Submitted by a student to request a wallet credit Transfer Request within a Teacher Platform.
/// Amount is in EGP. Payment method is a supported external mobile-payment channel; the transfer
/// reference is optional free-text metadata.
/// </summary>
public sealed record SubmitTransferRequest(
    [property: Required]
    decimal Amount,
    [property: Required]
    string PaymentMethod,
    string? TransferReference);
