using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Domain.Constants;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Infrastructure.Persistence.Seed;

// Development-only seed data: 1 Super Admin, 1 HR Admin, 2 Managers, 5 Employees.
// Never run against Staging/Production.
public static class DevelopmentSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        if (await db.Roles.AnyAsync())
        {
            return; // already seeded
        }

        var permissions = Permissions.All.Select(code => new Permission { Code = code }).ToList();
        db.Permissions.AddRange(permissions);

        var superAdminRole = new Role { Name = "SuperAdmin", Description = "Full system control" };
        var hrAdminRole = new Role { Name = "HRAdmin", Description = "Manages employees, attendance, leave, holidays, docs, announcements" };
        var managerRole = new Role { Name = "Manager", Description = "Team view and leave approvals" };
        var employeeRole = new Role { Name = "Employee", Description = "Self-service" };
        db.Roles.AddRange(superAdminRole, hrAdminRole, managerRole, employeeRole);

        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission { Role = superAdminRole, Permission = permission });
        }
        // HR Admin: manage employees, attendance, leave, holidays, documents, announcements; view reports.
        // Department/location/shift management is SuperAdmin-only per spec, so department.manage is deliberately excluded here.
        foreach (var code in new[]
                 {
                     Permissions.EmployeeView, Permissions.EmployeeManage, Permissions.AttendanceView, Permissions.AttendanceManage,
                     Permissions.AttendanceApprove, Permissions.LeaveView, Permissions.LeaveApprove, Permissions.LeaveTypeManage,
                     Permissions.AnnouncementManage, Permissions.HolidayManage, Permissions.DocumentManage, Permissions.FaceEnroll
                 })
        {
            db.RolePermissions.Add(new RolePermission { Role = hrAdminRole, Permission = permissions.First(p => p.Code == code) });
        }
        foreach (var code in new[] { Permissions.EmployeeView, Permissions.AttendanceView, Permissions.AttendanceApprove, Permissions.LeaveView, Permissions.LeaveApprove })
        {
            db.RolePermissions.Add(new RolePermission { Role = managerRole, Permission = permissions.First(p => p.Code == code) });
        }
        // Employee: self-service only. employee.view is deliberately excluded — it would unlock
        // browsing the entire company directory; self-access to own profile/documents is
        // handled by an explicit "own record" check in EmployeesController instead.
        foreach (var code in new[] { Permissions.AttendanceView, Permissions.LeaveView, Permissions.LeaveApply })
        {
            db.RolePermissions.Add(new RolePermission { Role = employeeRole, Permission = permissions.First(p => p.Code == code) });
        }

        var hrDept = new Department { Name = "HR" };
        var techDept = new Department { Name = "Technology" };
        var marketingDept = new Department { Name = "Marketing" };
        var financeDept = new Department { Name = "Finance" };
        db.Departments.AddRange(hrDept, techDept, marketingDept, financeDept);

        var hrManagerDesig = new Designation { Title = "HR Manager", Department = hrDept };
        var engManagerDesig = new Designation { Title = "Engineering Manager", Department = techDept };
        var softwareEngDesig = new Designation { Title = "Software Engineer", Department = techDept };
        var marketingExecDesig = new Designation { Title = "Marketing Executive", Department = marketingDept };
        var financeAnalystDesig = new Designation { Title = "Finance Analyst", Department = financeDept };
        db.Designations.AddRange(hrManagerDesig, engManagerDesig, softwareEngDesig, marketingExecDesig, financeAnalystDesig);

        var headOffice = new Location
        {
            Name = "Head Office",
            Address = "Development Seed Location",
            Latitude = 0,
            Longitude = 0,
            GeofenceRadiusMeters = 100
        };
        db.Locations.Add(headOffice);

        await db.SaveChangesAsync();

        string Hash(string pw) => passwordHasher.Hash(pw);

        var superAdmin = new Employee
        {
            EmployeeCode = "EMP0001", FirstName = "Super", LastName = "Admin", Email = "superadmin@carminepeople.dev",
            PasswordHash = Hash("Passw0rd!"), Department = hrDept, Designation = hrManagerDesig, Location = headOffice,
            Role = superAdminRole, DateOfJoining = DateTime.UtcNow.Date, EmploymentStatus = EmploymentStatus.Active
        };
        var hrAdmin = new Employee
        {
            EmployeeCode = "EMP0002", FirstName = "HR", LastName = "Admin", Email = "hradmin@carminepeople.dev",
            PasswordHash = Hash("Passw0rd!"), Department = hrDept, Designation = hrManagerDesig, Location = headOffice,
            Role = hrAdminRole, DateOfJoining = DateTime.UtcNow.Date, EmploymentStatus = EmploymentStatus.Active
        };
        var manager1 = new Employee
        {
            EmployeeCode = "EMP0003", FirstName = "Tech", LastName = "Manager", Email = "manager1@carminepeople.dev",
            PasswordHash = Hash("Passw0rd!"), Department = techDept, Designation = engManagerDesig, Location = headOffice,
            Role = managerRole, DateOfJoining = DateTime.UtcNow.Date, EmploymentStatus = EmploymentStatus.Active
        };
        var manager2 = new Employee
        {
            EmployeeCode = "EMP0004", FirstName = "Marketing", LastName = "Manager", Email = "manager2@carminepeople.dev",
            PasswordHash = Hash("Passw0rd!"), Department = marketingDept, Designation = marketingExecDesig, Location = headOffice,
            Role = managerRole, DateOfJoining = DateTime.UtcNow.Date, EmploymentStatus = EmploymentStatus.Active
        };
        db.Employees.AddRange(superAdmin, hrAdmin, manager1, manager2);
        await db.SaveChangesAsync();

        var employees = Enumerable.Range(1, 5).Select(i => new Employee
        {
            EmployeeCode = $"EMP{1004 + i:D4}",
            FirstName = "Employee",
            LastName = i.ToString(),
            Email = $"employee{i}@carminepeople.dev",
            PasswordHash = Hash("Passw0rd!"),
            Department = i % 2 == 0 ? techDept : marketingDept,
            Designation = i % 2 == 0 ? softwareEngDesig : marketingExecDesig,
            Location = headOffice,
            Role = employeeRole,
            Manager = i % 2 == 0 ? manager1 : manager2,
            DateOfJoining = DateTime.UtcNow.Date,
            EmploymentStatus = EmploymentStatus.Active
        }).ToList();
        db.Employees.AddRange(employees);
        await db.SaveChangesAsync();

        var leaveTypes = new[]
        {
            new LeaveType { Name = "Casual", IsPaid = true, DefaultAnnualDays = 12 },
            new LeaveType { Name = "Sick", IsPaid = true, DefaultAnnualDays = 10 },
            new LeaveType { Name = "Earned", IsPaid = true, DefaultAnnualDays = 15 },
            new LeaveType { Name = "Paid", IsPaid = true, DefaultAnnualDays = 5 },
            new LeaveType { Name = "Unpaid", IsPaid = false, DefaultAnnualDays = 0 },
            new LeaveType { Name = "WFH", IsPaid = true, RequiresAttachment = false, DefaultAnnualDays = 24 }
        };
        db.LeaveTypes.AddRange(leaveTypes);
        await db.SaveChangesAsync();

        var currentYear = DateTime.UtcNow.Year;
        var allEmployees = new[] { superAdmin, hrAdmin, manager1, manager2 }.Concat(employees);
        foreach (var emp in allEmployees)
        {
            foreach (var lt in leaveTypes.Where(lt => lt.DefaultAnnualDays > 0))
            {
                db.LeaveBalances.Add(new LeaveBalance
                {
                    EmployeeId = emp.Id,
                    LeaveTypeId = lt.Id,
                    Year = currentYear,
                    Available = lt.DefaultAnnualDays,
                    Used = 0,
                    Pending = 0
                });
            }
        }

        db.Holidays.Add(new Holiday { Name = "New Year's Day", Date = new DateOnly(currentYear, 1, 1) });
        db.Holidays.Add(new Holiday { Name = "Republic Day", Date = new DateOnly(currentYear, 1, 26) });
        db.Holidays.Add(new Holiday { Name = "Independence Day", Date = new DateOnly(currentYear, 8, 15) });

        // A day shift, assigned to every seeded employee — without this, the admin
        // dashboard's "Today's Schedule" and the attendance reminder job have nothing to
        // work against on a freshly-seeded database.
        var generalShift = new Shift
        {
            Name = "General Shift",
            Type = ShiftType.General,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(18, 0, 0),
            GracePeriodMinutes = 10,
            BreakMinutes = 60
        };
        db.Shifts.Add(generalShift);
        await db.SaveChangesAsync();

        var shiftEffectiveFrom = DateTime.UtcNow.Date.AddDays(-30);
        foreach (var emp in allEmployees)
        {
            db.EmployeeShifts.Add(new EmployeeShift { EmployeeId = emp.Id, ShiftId = generalShift.Id, EffectiveFrom = shiftEffectiveFrom });
        }

        // Attendance history: the last 5 working weekdays (excluding today, which is left
        // for the employee to check into live) per employee, mostly Present with an
        // occasional Late/Absent so dashboard trend charts and reports have varied data to
        // render on a freshly-seeded database rather than empty charts.
        var attendanceDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
        var seededDays = 0;
        var dayOffset = 0;
        while (seededDays < 5)
        {
            var date = attendanceDate.AddDays(-dayOffset);
            dayOffset++;
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            seededDays++;

            var employeeIndex = 0;
            foreach (var emp in employees)
            {
                var isLate = employeeIndex == 0 && seededDays == 2;
                var isAbsent = employeeIndex == 1 && seededDays == 3;
                employeeIndex++;
                if (isAbsent)
                {
                    db.AttendanceRecords.Add(new AttendanceRecord
                    {
                        EmployeeId = emp.Id,
                        Date = date,
                        Status = AttendanceStatus.Absent,
                        LocationId = headOffice.Id
                    });
                    continue;
                }

                var checkInHour = isLate ? 9 + generalShift.GracePeriodMinutes / 60 + 1 : 9;
                var checkInAt = date.ToDateTime(new TimeOnly(checkInHour, isLate ? 15 : 0));
                var checkOutAt = date.ToDateTime(new TimeOnly(18, 5));
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    EmployeeId = emp.Id,
                    Date = date,
                    CheckInAt = checkInAt,
                    CheckInLat = headOffice.Latitude,
                    CheckInLng = headOffice.Longitude,
                    CheckOutAt = checkOutAt,
                    Status = isLate ? AttendanceStatus.Late : AttendanceStatus.Present,
                    WorkingMinutes = (int)(checkOutAt - checkInAt).TotalMinutes - generalShift.BreakMinutes,
                    LocationId = headOffice.Id
                });
            }
        }

        // A couple of leave applications in different states, so the admin Leave page and
        // manager approval queue aren't empty on a freshly-seeded database.
        var casualLeaveType = leaveTypes.First(lt => lt.Name == "Casual");
        var pendingApplicant = employees[0];
        var pendingStart = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7));
        var pendingEnd = pendingStart.AddDays(1);
        db.LeaveApplications.Add(new LeaveApplication
        {
            EmployeeId = pendingApplicant.Id,
            LeaveTypeId = casualLeaveType.Id,
            StartDate = pendingStart,
            EndDate = pendingEnd,
            DaysCount = 2,
            Reason = "Personal errands",
            Status = LeaveApplicationStatus.Pending
        });
        db.LeaveBalances.First(b => b.EmployeeId == pendingApplicant.Id && b.LeaveTypeId == casualLeaveType.Id).Pending += 2;

        var approvedApplicant = employees[2];
        var approvedStart = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-10));
        db.LeaveApplications.Add(new LeaveApplication
        {
            EmployeeId = approvedApplicant.Id,
            LeaveTypeId = casualLeaveType.Id,
            StartDate = approvedStart,
            EndDate = approvedStart,
            DaysCount = 1,
            Reason = "Family function",
            Status = LeaveApplicationStatus.Approved
        });
        var approvedBalance = db.LeaveBalances.First(b => b.EmployeeId == approvedApplicant.Id && b.LeaveTypeId == casualLeaveType.Id);
        approvedBalance.Used += 1;

        // Announcements: one active, one already expired, so the mobile feed and admin
        // announcements list both have realistic content instead of an empty state.
        db.Announcements.Add(new Announcement
        {
            Title = "Welcome to Carmine People",
            Body = "This is the new HR platform for attendance, leave, and company updates. Reach out to HR if you have any questions.",
            AudienceType = AnnouncementAudienceType.All,
            PublishAt = DateTime.UtcNow.AddDays(-2),
            CreatedByEmployeeId = hrAdmin.Id
        });
        db.Announcements.Add(new Announcement
        {
            Title = "Office Wi-Fi maintenance (completed)",
            Body = "Scheduled maintenance on the office network completed successfully last week.",
            AudienceType = AnnouncementAudienceType.All,
            PublishAt = DateTime.UtcNow.AddDays(-14),
            ExpiresAt = DateTime.UtcNow.AddDays(-7),
            CreatedByEmployeeId = hrAdmin.Id
        });

        await db.SaveChangesAsync();
    }
}
