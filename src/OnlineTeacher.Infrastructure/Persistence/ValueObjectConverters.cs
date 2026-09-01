using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Email = OnlineTeacher.Domain.ValueObjects.Email;
using PublicId = OnlineTeacher.Domain.ValueObjects.PublicId;
using Slug = OnlineTeacher.Domain.ValueObjects.Slug;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// Shared value converters between domain value objects and string columns.
/// </summary>
public static class ValueObjectConverters
{
    public static readonly ValueConverter<Email, string> EmailConverter =
        new(value => value.Value, value => Email.Create(value));

    public static readonly ValueConverter<PublicId, string> PublicIdConverter =
        new(value => value.Value, value => PublicId.Create(value));

    public static readonly ValueConverter<Slug, string> SlugConverter =
        new(value => value.Value, value => Slug.Create(value));
}