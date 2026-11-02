using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrAdministration.Infrastructure.EventSourcing.Migrations
{
    /// <inheritdoc />
    public partial class CreateEventStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    stream_id = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => new { x.stream_id, x.version });
                });

            // The store is the audit trail (C17): the database itself refuses any change, whatever the code does.
            migrationBuilder.Sql("""
                CREATE FUNCTION events_refuse_change() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'The events table is append-only.';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER events_no_update_nor_delete BEFORE UPDATE OR DELETE ON events
                    FOR EACH ROW EXECUTE FUNCTION events_refuse_change();

                CREATE TRIGGER events_no_truncate BEFORE TRUNCATE ON events
                    FOR EACH STATEMENT EXECUTE FUNCTION events_refuse_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.Sql("DROP FUNCTION events_refuse_change();");
        }
    }
}
