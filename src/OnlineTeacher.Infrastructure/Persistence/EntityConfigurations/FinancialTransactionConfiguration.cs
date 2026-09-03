using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        builder.ToTable("financial_transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.WalletId).HasColumnName("wallet_id").IsRequired();
        builder.Property(t => t.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(t => t.Type).HasColumnName("type").IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").IsRequired();
        builder.Property(t => t.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.BalanceBefore).HasColumnName("balance_before").HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.BalanceAfter).HasColumnName("balance_after").HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.Reference).HasColumnName("reference").HasMaxLength(200);
        builder.Property(t => t.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(t => t.ActorType).HasColumnName("actor_type").HasMaxLength(50).IsRequired();
        builder.Property(t => t.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();

        builder.HasIndex(t => new { t.WalletId, t.OccurredAtUtc })
            .HasDatabaseName("ix_financial_transactions_wallet_occurred");
        builder.HasIndex(t => new { t.TenantId, t.OccurredAtUtc })
            .HasDatabaseName("ix_financial_transactions_tenant_occurred");

        builder.HasOne<StudentWallet>()
            .WithMany()
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_financial_transactions_wallet");

        builder.HasOne<TeacherPlatform>()
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_financial_transactions_tenant");
    }
}
