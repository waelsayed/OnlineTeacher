using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineTeacher.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseIdToStudentCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "course_id",
                table: "student_coupons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_student_coupons_course_id",
                table: "student_coupons",
                column: "course_id");

            migrationBuilder.AddForeignKey(
                name: "fk_student_coupons_course",
                table: "student_coupons",
                column: "course_id",
                principalTable: "courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_student_coupons_course",
                table: "student_coupons");

            migrationBuilder.DropIndex(
                name: "IX_student_coupons_course_id",
                table: "student_coupons");

            migrationBuilder.DropColumn(
                name: "course_id",
                table: "student_coupons");
        }
    }
}
