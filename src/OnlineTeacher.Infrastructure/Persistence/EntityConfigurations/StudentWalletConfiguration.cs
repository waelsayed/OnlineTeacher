using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class StudentWalletConfiguration : IEntityTypeConfiguration<StudentWallet>
{
    public void Configure(EntityTypeBuilder<StudentWallet> builder)
    {
        builder.ToTable("student_wallets");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(w => w.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(w => w.Balance).HasColumnName("balance").HasPrecision(18, 2).IsRequired();

        builder.HasIndex(w => new { w.StudentId, w.TenantId })
            .IsUnique()
            .HasDatabaseName("ux_student_wallets_student_tenant");
        builder.HasIndex(w => new { w.TenantId, w.StudentId })
            .HasDatabaseName("ix_student_wallets_tenant_student");

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(w => w.StudentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_student_wallets_student");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(w => w.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_student_wallets_tenant");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}
