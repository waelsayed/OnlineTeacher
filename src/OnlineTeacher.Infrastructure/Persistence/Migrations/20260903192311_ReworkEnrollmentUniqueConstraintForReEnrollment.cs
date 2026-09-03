using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineTeacher.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReworkEnrollmentUniqueConstraintForReEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_enrollments_student_course",
                table: "enrollments");

            migrationBuilder.CreateIndex(
                name: "ux_enrollments_student_course",
                table: "enrollments",
                columns: new[] { "student_id", "course_id" },
                unique: true,
                filter: "status = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_enrollments_student_course",
                table: "enrollments");

            migrationBuilder.CreateIndex(
                name: "ux_enrollments_student_course",
                table: "enrollments",
                columns: new[] { "student_id", "course_id" },
                unique: true);
        }
    }
}
