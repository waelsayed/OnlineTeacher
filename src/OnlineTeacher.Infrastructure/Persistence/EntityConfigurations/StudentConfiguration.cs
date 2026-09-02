using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence.EntityConfigurations;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(s => s.Email)
            .HasColumnName("email")
            .HasConversion(ValueObjectConverters.EmailConverter)
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(s => s.Email).IsUnique().HasDatabaseName("ux_students_email");

        builder.Property(s => s.PasswordHash).HasColumnName("password_hash").IsRequired();

        builder.HasMany(s => s.Follows)
            .WithOne()
            .HasForeignKey(f => f.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        TeacherConfiguration.ConfigureAudit(builder);
    }
}