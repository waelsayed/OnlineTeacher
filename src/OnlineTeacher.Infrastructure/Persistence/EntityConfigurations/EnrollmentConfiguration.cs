using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(e => e.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").IsRequired();
        builder.Property(e => e.EnrolledAtUtc).HasColumnName("enrolled_at_utc").IsRequired();
        builder.Property(e => e.CancelledAtUtc).HasColumnName("cancelled_at_utc");

        // Partial unique index: a student may hold only ONE Active enrollment per course, but
        // historical terminal (cancelled) enrollments may coexist so a student can re-enroll after
        // the previous enrollment reached its terminal state (approved requirement).
        builder.HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique()
            .HasFilter($"status = {(int)EnrollmentStatus.Active}")
            .HasDatabaseName("ux_enrollments_student_course");
        builder.HasIndex(e => new { e.StudentId, e.TenantId })
            .HasDatabaseName("ix_enrollments_student_tenant");
        builder.HasIndex(e => new { e.CourseId, e.TenantId })
            .HasDatabaseName("ix_enrollments_course_tenant");

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_enrollments_student");

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_enrollments_course");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_enrollments_tenant");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}
