using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfig : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        // Prevents duplicate check-in for the same employee/day at the database level.
        builder.HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();

        // Company-wide "today"/date-range queries (dashboard KPIs, reports, the attendance
        // reminder job) filter by Date alone across all employees — the composite index above
        // only helps when EmployeeId is also part of the predicate, so this covers the
        // cross-employee case separately.
        builder.HasIndex(a => a.Date);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Location)
            .WithMany()
            .HasForeignKey(a => a.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Breaks)
            .WithOne(b => b.AttendanceRecord)
            .HasForeignKey(b => b.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
