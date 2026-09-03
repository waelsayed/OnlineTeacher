using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(u => u.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(u => u.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(u => u.Position).HasColumnName("position").IsRequired();

        builder.HasIndex(u => new { u.CourseId, u.Position })
            .HasDatabaseName("ux_units_course_position");
        builder.HasIndex(u => u.CourseId).HasDatabaseName("ix_units_course");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_units_tenant");

        builder.HasMany(u => u.Lessons)
            .WithOne()
            .HasForeignKey(l => l.UnitId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_lessons_unit");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}