using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(50).IsRequired();

        builder.HasIndex(r => new { r.TenantId, r.Name }).IsUnique().HasDatabaseName("ux_roles_tenant_name");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_roles_tenant");

        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_role_permissions_role");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}