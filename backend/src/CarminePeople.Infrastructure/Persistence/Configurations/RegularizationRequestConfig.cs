using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class RegularizationRequestConfig : IEntityTypeConfiguration<RegularizationRequest>
{
    public void Configure(EntityTypeBuilder<RegularizationRequest> builder)
    {
        builder.HasOne(r => r.AttendanceRecord)
            .WithMany()
            .HasForeignKey(r => r.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.ApprovalTrail)
            .WithOne(a => a.RegularizationRequest)
            .HasForeignKey(a => a.RegularizationRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RegularizationApprovalConfig : IEntityTypeConfiguration<RegularizationApproval>
{
    public void Configure(EntityTypeBuilder<RegularizationApproval> builder)
    {
        builder.HasOne(a => a.Approver)
            .WithMany()
            .HasForeignKey(a => a.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
