using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class StudentFollowConfiguration : IEntityTypeConfiguration<StudentFollow>
{
    public void Configure(EntityTypeBuilder<StudentFollow> builder)
    {
        builder.ToTable("student_follows");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(f => f.TeacherId).HasColumnName("teacher_id").IsRequired();

        builder.HasIndex(f => new { f.StudentId, f.TeacherId })
            .IsUnique()
            .HasDatabaseName("ux_follows_student_teacher");
        builder.HasIndex(f => f.TeacherId).HasDatabaseName("ix_follows_teacher");

        builder.HasOne<Teacher>()
            .WithMany()
            .HasForeignKey(f => f.TeacherId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_follows_teacher");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}