using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class OvertimeRequestConfig : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.Property(o => o.Hours).HasPrecision(5, 2);

        builder.HasOne(o => o.Employee).WithMany().HasForeignKey(o => o.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.DecidedByEmployee).WithMany().HasForeignKey(o => o.DecidedByEmployeeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.EmployeeId, o.Date });
    }
}
