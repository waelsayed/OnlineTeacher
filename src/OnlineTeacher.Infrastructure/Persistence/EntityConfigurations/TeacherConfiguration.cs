using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("teachers");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(t => t.Email)
            .HasColumnName("email")
            .HasConversion(ValueObjectConverters.EmailConverter)
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(t => t.Email).IsUnique().HasDatabaseName("ux_teachers_email");

        builder.Property(t => t.PasswordHash).HasColumnName("password_hash").IsRequired();

        builder.HasMany(t => t.Memberships)
            .WithOne()
            .HasForeignKey(m => m.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAudit(builder);
    }

    internal static void ConfigureAudit<T>(EntityTypeBuilder<T> builder)
        where T : class, OnlineTeacher.Domain.Common.IAuditable
    {
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
    }
}