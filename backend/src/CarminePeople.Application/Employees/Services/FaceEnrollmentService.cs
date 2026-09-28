using System.Text.Json;
using CarminePeople.Application.Attendance.DTOs;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Employees.Services;

public class FaceEnrollmentService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public FaceEnrollmentService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<Result<FaceEnrollmentStatusDto>> EnrollAsync(Guid employeeId, FaceEnrollmentRequest request, Guid actorEmployeeId, CancellationToken ct)
    {
        if (request.Embedding is not ({ Length: 128 } or { Length: 192 }))
        {
            return Result<FaceEnrollmentStatusDto>.Fail("INVALID_EMBEDDING", "Embedding must be a 128 or 192-dimensional vector.");
        }

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Result<FaceEnrollmentStatusDto>.Fail("NOT_FOUND", "Employee not found.");

        employee.FaceEmbedding = JsonSerializer.Serialize(request.Embedding);
        employee.FaceEnrolledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLogger.LogAsync(actorEmployeeId, "employee.face_enrolled", "Employee", employee.Id.ToString(), null, ct);

        return Result<FaceEnrollmentStatusDto>.Ok(new FaceEnrollmentStatusDto(true, employee.FaceEnrolledAt));
    }

    public async Task<Result<FaceEnrollmentStatusDto>> GetStatusAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Result<FaceEnrollmentStatusDto>.Fail("NOT_FOUND", "Employee not found.");

        return Result<FaceEnrollmentStatusDto>.Ok(new FaceEnrollmentStatusDto(employee.FaceEnrolledAt is not null, employee.FaceEnrolledAt));
    }
}
