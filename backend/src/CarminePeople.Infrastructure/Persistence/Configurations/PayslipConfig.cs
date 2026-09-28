using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class PayslipConfig : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.Property(p => p.GrossAmount).HasPrecision(12, 2);
        builder.Property(p => p.DeductionsAmount).HasPrecision(12, 2);
        builder.Property(p => p.NetAmount).HasPrecision(12, 2);

        builder.HasOne(p => p.Employee).WithMany().HasForeignKey(p => p.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.EmployeeId, p.Year, p.Month }).IsUnique();
    }
}
