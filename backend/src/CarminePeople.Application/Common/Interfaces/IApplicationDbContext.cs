using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Department> Departments { get; }
    DbSet<Designation> Designations { get; }
    DbSet<Location> Locations { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<EmployeeShift> EmployeeShifts { get; }
    DbSet<EmployeeMonthlyLateCounter> EmployeeMonthlyLateCounters { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<Break> Breaks { get; }
    DbSet<RegularizationRequest> RegularizationRequests { get; }
    DbSet<RegularizationApproval> RegularizationApprovals { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<LeaveApplication> LeaveApplications { get; }
    DbSet<LeaveApproval> LeaveApprovals { get; }
    DbSet<OvertimeRequest> OvertimeRequests { get; }
    DbSet<ShiftSwapRequest> ShiftSwapRequests { get; }
    DbSet<Payslip> Payslips { get; }
    DbSet<AnnouncementComment> AnnouncementComments { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<DeviceToken> DeviceTokens { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<BrandingSettings> BrandingSettings { get; }
    DbSet<Asset> Assets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
