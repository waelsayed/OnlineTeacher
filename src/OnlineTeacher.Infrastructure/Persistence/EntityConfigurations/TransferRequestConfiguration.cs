using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class TransferRequestConfiguration : IEntityTypeConfiguration<TransferRequest>
{
    public void Configure(EntityTypeBuilder<TransferRequest> builder)
    {
        builder.ToTable("transfer_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.WalletId).HasColumnName("wallet_id").IsRequired();
        builder.Property(r => r.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(r => r.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(r => r.PaymentMethod).HasColumnName("payment_method").IsRequired();
        builder.Property(r => r.TransferReference).HasColumnName("transfer_reference").HasMaxLength(200);
        builder.Property(r => r.Status).HasColumnName("status").IsRequired();

        builder.HasIndex(r => new { r.TenantId, r.Status })
            .HasDatabaseName("ix_transfer_requests_tenant_status");
        builder.HasIndex(r => new { r.WalletId, r.Status })
            .HasDatabaseName("ix_transfer_requests_wallet_status");

        builder.HasOne<StudentWallet>()
            .WithMany()
            .HasForeignKey(r => r.WalletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_transfer_requests_wallet");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_transfer_requests_tenant");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}
