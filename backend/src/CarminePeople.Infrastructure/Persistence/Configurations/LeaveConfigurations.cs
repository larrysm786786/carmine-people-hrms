using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarminePeople.Infrastructure.Persistence.Configurations;

public class LeaveBalanceConfig : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.HasIndex(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year }).IsUnique();
        builder.Property(lb => lb.Available).HasPrecision(6, 2);
        builder.Property(lb => lb.Used).HasPrecision(6, 2);
        builder.Property(lb => lb.Pending).HasPrecision(6, 2);
        builder.Property(lb => lb.RowVersion).IsRowVersion();

        builder.HasOne(lb => lb.Employee).WithMany().HasForeignKey(lb => lb.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(lb => lb.LeaveType).WithMany().HasForeignKey(lb => lb.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveApplicationConfig : IEntityTypeConfiguration<LeaveApplication>
{
    public void Configure(EntityTypeBuilder<LeaveApplication> builder)
    {
        builder.Property(la => la.DaysCount).HasPrecision(6, 2);

        builder.HasOne(la => la.Employee).WithMany().HasForeignKey(la => la.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(la => la.LeaveType).WithMany().HasForeignKey(la => la.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(la => la.ApprovalTrail)
            .WithOne(a => a.LeaveApplication)
            .HasForeignKey(a => a.LeaveApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LeaveApprovalConfig : IEntityTypeConfiguration<LeaveApproval>
{
    public void Configure(EntityTypeBuilder<LeaveApproval> builder)
    {
        builder.HasOne(a => a.Approver).WithMany().HasForeignKey(a => a.ApproverId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class HolidayConfig : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.HasOne(h => h.Location).WithMany().HasForeignKey(h => h.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(h => h.Date);
    }
}
