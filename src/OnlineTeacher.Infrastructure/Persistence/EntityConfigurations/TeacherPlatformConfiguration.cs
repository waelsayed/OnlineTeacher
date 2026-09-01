using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class TeacherPlatformConfiguration : IEntityTypeConfiguration<TeacherPlatform>
{
    public void Configure(EntityTypeBuilder<TeacherPlatform> builder)
    {
        builder.ToTable("teacher_platforms");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(p => p.PublicId)
            .HasColumnName("public_id")
            .HasConversion(ValueObjectConverters.PublicIdConverter)
            .HasMaxLength(12)
            .IsRequired();
        builder.HasIndex(p => p.PublicId).IsUnique().HasDatabaseName("ux_teacher_platforms_public_id");

        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .HasConversion(ValueObjectConverters.SlugConverter)
            .HasMaxLength(60)
            .IsRequired();
        builder.HasIndex(p => p.Slug).HasDatabaseName("ix_teacher_platforms_slug");

        builder.Property(p => p.Status).HasColumnName("status").IsRequired();
        builder.Property(p => p.ActivatedAtUtc).HasColumnName("activated_at_utc");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}