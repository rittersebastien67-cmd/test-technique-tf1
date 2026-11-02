using HrAdministration.Infrastructure.Employees;
using HrAdministration.Infrastructure.Projections;
using Microsoft.EntityFrameworkCore;

namespace HrAdministration.Infrastructure.EventSourcing;

public sealed class HrAdministrationDbContext : DbContext
{
    public HrAdministrationDbContext(DbContextOptions<HrAdministrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<EventRecord> Events => Set<EventRecord>();

    public DbSet<LeaveRequestViewRecord> LeaveRequestsView => Set<LeaveRequestViewRecord>();

    public DbSet<BalanceViewRecord> BalancesView => Set<BalanceViewRecord>();

    public DbSet<EmployeeProfileRecord> EmployeeProfiles => Set<EmployeeProfileRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventRecord>(entity =>
        {
            entity.ToTable("events");

            // The key is the unique constraint on (stream_id, version) that optimistic concurrency relies on.
            entity.HasKey(record => new { record.StreamId, record.Version }).HasName("pk_events");

            entity.Property(record => record.StreamId).HasColumnName("stream_id");
            entity.Property(record => record.Version).HasColumnName("version");
            entity.Property(record => record.Type).HasColumnName("type");
            entity.Property(record => record.Data).HasColumnName("data").HasColumnType("jsonb");
            entity.Property(record => record.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            entity.Property(record => record.RecordedAt).HasColumnName("recorded_at");
        });

        modelBuilder.Entity<LeaveRequestViewRecord>(entity =>
        {
            entity.ToTable("leave_requests_view");

            entity.HasKey(record => record.LeaveRequestId).HasName("pk_leave_requests_view");
            entity.HasIndex(record => record.EmployeeId).HasDatabaseName("ix_leave_requests_view_employee_id");

            entity.Property(record => record.LeaveRequestId).HasColumnName("leave_request_id").ValueGeneratedNever();
            entity.Property(record => record.EmployeeId).HasColumnName("employee_id");
            entity.Property(record => record.Origin).HasColumnName("origin").HasConversion<string>();
            entity.Property(record => record.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(record => record.Type).HasColumnName("type");
            entity.Property(record => record.StartDate).HasColumnName("start_date");
            entity.Property(record => record.StartHalfDay).HasColumnName("start_half_day").HasConversion<string>();
            entity.Property(record => record.EndDate).HasColumnName("end_date");
            entity.Property(record => record.EndHalfDay).HasColumnName("end_half_day").HasConversion<string>();
            entity.Property(record => record.Comment).HasColumnName("comment");
            entity.Property(record => record.Duration).HasColumnName("duration").HasColumnType("jsonb");
            entity.Property(record => record.Flags).HasColumnName("flags").HasColumnType("jsonb");
            entity.Property(record => record.Warnings).HasColumnName("warnings").HasColumnType("jsonb");
        });

        modelBuilder.Entity<BalanceViewRecord>(entity =>
        {
            entity.ToTable("balances_view");

            entity.HasKey(record => record.StreamId).HasName("pk_balances_view");
            entity.HasIndex(record => record.EmployeeId).HasDatabaseName("ix_balances_view_employee_id");

            entity.Property(record => record.StreamId).HasColumnName("stream_id");
            entity.Property(record => record.EmployeeId).HasColumnName("employee_id");
            entity.Property(record => record.Type).HasColumnName("type").HasConversion<string>();
            entity.Property(record => record.Year).HasColumnName("year");
            entity.Property(record => record.Acquired).HasColumnName("acquired");
            entity.Property(record => record.CurrentYearOnly).HasColumnName("current_year_only");
            entity.Property(record => record.Reservations).HasColumnName("reservations").HasColumnType("jsonb");
        });

        modelBuilder.Entity<EmployeeProfileRecord>(entity =>
        {
            entity.ToTable("employee_profiles");

            entity.HasKey(record => record.EmployeeId).HasName("pk_employee_profiles");

            entity.Property(record => record.EmployeeId).HasColumnName("employee_id").ValueGeneratedNever();
            entity.Property(record => record.FirstName).HasColumnName("first_name");
            entity.Property(record => record.LastName).HasColumnName("last_name");
            entity.Property(record => record.JobTitle).HasColumnName("job_title");
            entity.Property(record => record.ManagerFirstName).HasColumnName("manager_first_name");
            entity.Property(record => record.ManagerLastName).HasColumnName("manager_last_name");
            entity.Property(record => record.WorkSchedule).HasColumnName("work_schedule");
            entity.Property(record => record.WorkLocation).HasColumnName("work_location");
        });
    }
}
