using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineTeacher.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "student_coupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    discount_type = table.Column<int>(type: "integer", nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    assigned_to_student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    consumed_in_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_coupons", x => x.Id);
                    table.ForeignKey(
                        name: "fk_student_coupons_student",
                        column: x => x.assigned_to_student_id,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_coupons_tenant",
                        column: x => x.tenant_id,
                        principalTable: "teacher_platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_coupons_transaction",
                        column: x => x.consumed_in_transaction_id,
                        principalTable: "financial_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_student_coupons_assigned_to_student_id",
                table: "student_coupons",
                column: "assigned_to_student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_coupons_consumed_in_transaction_id",
                table: "student_coupons",
                column: "consumed_in_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_coupons_status",
                table: "student_coupons",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_student_coupons_tenant",
                table: "student_coupons",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_student_coupons_tenant_code",
                table: "student_coupons",
                columns: new[] { "tenant_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_coupons");
        }
    }
}
