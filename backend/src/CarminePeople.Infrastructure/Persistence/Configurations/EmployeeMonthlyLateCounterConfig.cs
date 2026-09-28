using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class EmployeeMonthlyLateCounterConfig : IEntityTypeConfiguration<EmployeeMonthlyLateCounter>
{
    public void Configure(EntityTypeBuilder<EmployeeMonthlyLateCounter> builder)
    {
        builder.HasOne(c => c.Employee)
            .WithMany()
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // One counter row per employee per calendar month.
        builder.HasIndex(c => new { c.EmployeeId, c.Year, c.Month }).IsUnique();
    }
}
