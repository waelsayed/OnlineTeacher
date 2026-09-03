using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class StudentCouponConfiguration : IEntityTypeConfiguration<StudentCoupon>
{
    public void Configure(EntityTypeBuilder<StudentCoupon> builder)
    {
        builder.ToTable("student_coupons");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(c => c.Code).HasColumnName("code").IsRequired().HasMaxLength(100);
        builder.Property(c => c.DiscountType).HasColumnName("discount_type").IsRequired();
        builder.Property(c => c.DiscountValue).HasColumnName("discount_value").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(c => c.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").IsRequired();
        builder.Property(c => c.AssignedToStudentId).HasColumnName("assigned_to_student_id").IsRequired();
        builder.Property(c => c.ConsumedAt).HasColumnName("consumed_at");
        builder.Property(c => c.ConsumedInTransactionId).HasColumnName("consumed_in_transaction_id");
        builder.Property(c => c.CreatedByTeacherId).HasColumnName("created_by_teacher_id").IsRequired();

        // Unique index: coupon code is unique per tenant
        builder.HasIndex(c => new { c.TenantId, c.Code })
            .IsUnique()
            .HasDatabaseName("ux_student_coupons_tenant_code");

        builder.HasIndex(c => c.TenantId)
            .HasDatabaseName("ix_student_coupons_tenant");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("ix_student_coupons_status");

        // FK: AssignedToStudentId -> students(Id)
        builder.HasOne<OnlineTeacher.Domain.Entities.Student>()
            .WithMany()
            .HasForeignKey(c => c.AssignedToStudentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_student_coupons_student");

        // FK: ConsumedInTransactionId -> financial_transactions(Id)
        builder.HasOne<OnlineTeacher.Domain.Entities.FinancialTransaction>()
            .WithMany()
            .HasForeignKey(c => c.ConsumedInTransactionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_student_coupons_transaction");

        // FK: TenantId -> teacher_platforms(Id)
        builder.HasOne<OnlineTeacher.Domain.Entities.TeacherPlatform>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_student_coupons_tenant");

        TeacherConfiguration.ConfigureAudit(builder);
    }
}