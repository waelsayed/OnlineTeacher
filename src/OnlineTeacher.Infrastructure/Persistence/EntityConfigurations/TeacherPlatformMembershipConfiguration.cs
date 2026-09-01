using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class TeacherPlatformMembershipConfiguration : IEntityTypeConfiguration<TeacherPlatformMembership>
{
    public void Configure(EntityTypeBuilder<TeacherPlatformMembership> builder)
    {
        builder.ToTable("teacher_platform_memberships");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.TeacherId).HasColumnName("teacher_id").IsRequired();
        builder.Property(m => m.TeacherPlatformId).HasColumnName("teacher_platform_id").IsRequired();
        builder.Property(m => m.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(m => m.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(m => m.IsOwner).HasColumnName("is_owner").IsRequired();

        builder.HasIndex(m => new { m.TeacherId, m.TeacherPlatformId })
            .IsUnique()
            .HasDatabaseName("ux_memberships_teacher_platform");
        builder.HasIndex(m => m.TeacherPlatformId).HasDatabaseName("ix_memberships_teacher_platform");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(m => m.TeacherPlatformId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_memberships_teacher_platform");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_memberships_tenant");

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(m => m.RoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_memberships_role");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}