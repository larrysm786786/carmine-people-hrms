using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        // Filtered so a soft-deleted employee's Email/EmployeeCode doesn't permanently block
        // reuse (e.g. re-hiring, or reissuing a deleted employee's code) — without the
        // filter, EmployeeService's uniqueness checks (which respect the IsDeleted query
        // filter) would report the value as free while the DB-level index still rejects the
        // insert with an unhandled DbUpdateException.
        builder.HasIndex(e => e.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(e => e.EmployeeCode).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.EmployeeCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.MonthlySalary).HasPrecision(12, 2);

        builder.HasOne(e => e.Manager)
            .WithMany()
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Department)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Designation)
            .WithMany()
            .HasForeignKey(e => e.DesignationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Location)
            .WithMany()
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Role)
            .WithMany()
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
