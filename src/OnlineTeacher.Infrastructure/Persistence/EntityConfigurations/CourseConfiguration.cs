using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Summary).HasColumnName("summary").HasMaxLength(2000);
        builder.Property(c => c.Status).HasColumnName("status").IsRequired();
        builder.Property(c => c.PricingType).HasColumnName("pricing_type").IsRequired();
        builder.Property(c => c.Price).HasColumnName("price").HasPrecision(18, 2);

        builder.HasIndex(c => new { c.TenantId, c.Id }).HasDatabaseName("ix_courses_tenant");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_courses_tenant");

        builder.HasMany(c => c.Units)
            .WithOne()
            .HasForeignKey(u => u.CourseId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_units_course");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}