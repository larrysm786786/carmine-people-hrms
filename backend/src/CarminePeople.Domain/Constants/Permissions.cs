namespace CarminePeople.Domain.Constants;

// The fixed set of permission codes the system understands. Used both as the seed source
// (DevelopmentSeeder/PermissionSyncSeeder) and as [Authorize(Policy = ...)] targets, so a
// typo here is a compile error instead of a silently-never-matching policy string.
public static class Permissions
{
    public const string EmployeeView = "employee.view";
    public const string EmployeeManage = "employee.manage";

    public const string AttendanceView = "attendance.view";
    public const string AttendanceManage = "attendance.manage";
    public const string AttendanceApprove = "attendance.approve";

    public const string LeaveView = "leave.view";
    public const string LeaveApply = "leave.apply";
    public const string LeaveApprove = "leave.approve";
    public const string LeaveTypeManage = "leave_type.manage";

    public const string DepartmentManage = "department.manage";
    public const string AnnouncementManage = "announcement.manage";
    public const string AuditView = "audit.view";
    public const string HolidayManage = "holiday.manage";
    public const string DocumentManage = "document.manage";
    public const string RoleManage = "role.manage";
    public const string CompanyManage = "company.manage";
    public const string FaceEnroll = "face.enroll";

    public const string OvertimeApply = "overtime.apply";
    public const string OvertimeApprove = "overtime.approve";

    public const string ShiftSwapApply = "shift_swap.apply";
    public const string ShiftSwapApprove = "shift_swap.approve";

    public const string PayrollManage = "payroll.manage";
    public const string PayrollView = "payroll.view";

    public const string AnnouncementComment = "announcement.comment";

    public static readonly string[] All =
    {
        EmployeeView, EmployeeManage,
        AttendanceView, AttendanceManage, AttendanceApprove,
        LeaveView, LeaveApply, LeaveApprove, LeaveTypeManage,
        DepartmentManage, AnnouncementManage, AuditView,
        HolidayManage, DocumentManage, RoleManage, CompanyManage, FaceEnroll,
        OvertimeApply, OvertimeApprove, ShiftSwapApply, ShiftSwapApprove,
        PayrollManage, PayrollView, AnnouncementComment
    };
}
