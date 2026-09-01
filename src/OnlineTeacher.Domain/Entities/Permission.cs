using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// Global permission definition identified by a unique permission code.
/// The open-ended set of permission codes is the backbone of the dynamic Role + Permission authorization model.
/// </summary>
public sealed class Permission : IAuditable
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private Permission()
    {
    }

    public Permission(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Permission code is required.");
        }

        Id = Guid.NewGuid();
        Code = code.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }
}