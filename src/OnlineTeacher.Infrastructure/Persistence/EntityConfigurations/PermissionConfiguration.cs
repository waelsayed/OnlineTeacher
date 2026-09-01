using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique().HasDatabaseName("ux_permissions_code");

        builder.HasMany<RolePermission>()
            .WithOne()
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_role_permissions_permission");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}