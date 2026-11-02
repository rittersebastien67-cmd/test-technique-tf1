using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrAdministration.Infrastructure.EventSourcing.Migrations
{
    /// <inheritdoc />
    public partial class CreateProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "balances_view",
                columns: table => new
                {
                    stream_id = table.Column<string>(type: "text", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    acquired = table.Column<decimal>(type: "numeric", nullable: false),
                    current_year_only = table.Column<bool>(type: "boolean", nullable: false),
                    reservations = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_balances_view", x => x.stream_id);
                });

            migrationBuilder.CreateTable(
                name: "employee_profiles",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    job_title = table.Column<string>(type: "text", nullable: false),
                    manager_first_name = table.Column<string>(type: "text", nullable: false),
                    manager_last_name = table.Column<string>(type: "text", nullable: false),
                    work_schedule = table.Column<string>(type: "text", nullable: false),
                    work_location = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_profiles", x => x.employee_id);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests_view",
                columns: table => new
                {
                    leave_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_half_day = table.Column<string>(type: "text", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_half_day = table.Column<string>(type: "text", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    duration = table.Column<string>(type: "jsonb", nullable: true),
                    flags = table.Column<string>(type: "jsonb", nullable: false),
                    warnings = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_requests_view", x => x.leave_request_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_balances_view_employee_id",
                table: "balances_view",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_view_employee_id",
                table: "leave_requests_view",
                column: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "balances_view");

            migrationBuilder.DropTable(
                name: "employee_profiles");

            migrationBuilder.DropTable(
                name: "leave_requests_view");
        }
    }
}
