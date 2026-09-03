using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(l => l.UnitId).HasColumnName("unit_id").IsRequired();
        builder.Property(l => l.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(l => l.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(l => l.Position).HasColumnName("position").IsRequired();

        builder.HasIndex(l => new { l.UnitId, l.Position })
            .HasDatabaseName("ux_lessons_unit_position");
        builder.HasIndex(l => l.UnitId).HasDatabaseName("ix_lessons_unit");

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_lessons_course");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_lessons_tenant");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}