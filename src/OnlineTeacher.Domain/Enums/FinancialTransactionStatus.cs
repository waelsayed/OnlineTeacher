namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// Explicit state of a financial transaction. Recording a ledger entry is not the same as the
/// transaction having completed successfully; the status makes the outcome explicit.
/// </summary>
public enum FinancialTransactionStatus
{
    /// <summary>The transaction completed successfully and is final.</summary>
    Completed = 0
}
