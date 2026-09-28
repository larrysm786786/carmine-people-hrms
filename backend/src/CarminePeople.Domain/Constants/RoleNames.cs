namespace CarminePeople.Domain.Constants;

// Centralizes the "is this a privileged (HR/SuperAdmin) role" check that several services
// need to bypass manager-scoping rules (e.g. deciding any employee's regularization/leave
// request, not just direct reports). Previously duplicated as an inline
// `Role.Name is "SuperAdmin" or "HRAdmin"` string check in three separate places — a role
// rename or typo in one location wouldn't have been caught by the others.
public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string HRAdmin = "HRAdmin";

    public static bool IsPrivileged(string? roleName) => roleName is SuperAdmin or HRAdmin;
}
