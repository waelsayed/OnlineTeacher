namespace OnlineTeacher.Application.Dtos;

/// <summary>Summary of a student's wallet balance within a Teacher Platform.</summary>
public sealed record WalletResponse(
    Guid WalletId,
    decimal Balance,
    string Currency);
