using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(rp => rp.PermissionId).HasColumnName("permission_id").IsRequired();
        builder.Property(rp => rp.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique()
            .HasDatabaseName("ux_role_permissions_role_permission");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(rp => rp.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_role_permissions_tenant");
    }
}