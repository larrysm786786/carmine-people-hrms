using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Onboarding.DTOs;
using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Onboarding.Services;

public class OnboardingService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public OnboardingService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<List<OnboardingTaskTemplateDto>> GetTemplatesAsync(CancellationToken ct) =>
        await _db.OnboardingTaskTemplates.AsNoTracking()
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Title)
            .Select(t => new OnboardingTaskTemplateDto(t.Id, t.Title, t.Description, t.SortOrder))
            .ToListAsync(ct);

    public async Task<OnboardingTaskTemplateDto> CreateTemplateAsync(CreateOnboardingTaskTemplateRequest request, Guid actorId, CancellationToken ct)
    {
        var template = new OnboardingTaskTemplate
        {
            Title = request.Title,
            Description = request.Description,
            SortOrder = request.SortOrder,
            CreatedBy = actorId,
        };
        _db.OnboardingTaskTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "onboarding.template_created", "OnboardingTaskTemplate", template.Id.ToString(), null, ct);
        return new OnboardingTaskTemplateDto(template.Id, template.Title, template.Description, template.SortOrder);
    }

    public async Task<Result<OnboardingTaskTemplateDto>> UpdateTemplateAsync(Guid id, UpdateOnboardingTaskTemplateRequest request, Guid actorId, CancellationToken ct)
    {
        var template = await _db.OnboardingTaskTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (template is null) return Result<OnboardingTaskTemplateDto>.Fail("NOT_FOUND", "Checklist template not found.");

        template.Title = request.Title;
        template.Description = request.Description;
        template.SortOrder = request.SortOrder;
        template.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "onboarding.template_updated", "OnboardingTaskTemplate", template.Id.ToString(), null, ct);
        return Result<OnboardingTaskTemplateDto>.Ok(new OnboardingTaskTemplateDto(template.Id, template.Title, template.Description, template.SortOrder));
    }

    public async Task<Result<bool>> DeleteTemplateAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var template = await _db.OnboardingTaskTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (template is null) return Result<bool>.Fail("NOT_FOUND", "Checklist template not found.");

        template.IsDeleted = true;
        template.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "onboarding.template_deleted", "OnboardingTaskTemplate", template.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<List<OnboardingTaskDto>> GetTasksForEmployeeAsync(Guid employeeId, CancellationToken ct) =>
        await _db.OnboardingTasks.AsNoTracking()
            .Where(t => t.EmployeeId == employeeId)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Title)
            .Select(t => new OnboardingTaskDto(
                t.Id, t.EmployeeId, t.Employee.FirstName + " " + t.Employee.LastName, t.Title, t.Description, t.SortOrder, t.IsCompleted, t.CompletedAt))
            .ToListAsync(ct);

    // Copies every active template onto the employee's checklist, skipping titles they
    // already have so this is safe to run again (e.g. after a new template is added).
    public async Task<Result<int>> ApplyTemplatesAsync(Guid employeeId, Guid actorId, CancellationToken ct)
    {
        if (!await _db.Employees.AnyAsync(e => e.Id == employeeId, ct))
        {
            return Result<int>.Fail("NOT_FOUND", "Employee not found.");
        }

        var templates = await _db.OnboardingTaskTemplates.AsNoTracking().ToListAsync(ct);
        var existingTitles = await _db.OnboardingTasks.Where(t => t.EmployeeId == employeeId).Select(t => t.Title).ToListAsync(ct);
        var toAdd = templates.Where(t => !existingTitles.Contains(t.Title)).ToList();

        foreach (var template in toAdd)
        {
            _db.OnboardingTasks.Add(new OnboardingTask
            {
                EmployeeId = employeeId,
                Title = template.Title,
                Description = template.Description,
                SortOrder = template.SortOrder,
                CreatedBy = actorId,
            });
        }

        if (toAdd.Count > 0) await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "onboarding.templates_applied", "Employee", employeeId.ToString(), new { added = toAdd.Count }, ct);
        return Result<int>.Ok(toAdd.Count);
    }

    public async Task<Result<OnboardingTaskDto>> AddTaskAsync(Guid employeeId, CreateOnboardingTaskRequest request, Guid actorId, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Result<OnboardingTaskDto>.Fail("NOT_FOUND", "Employee not found.");

        var task = new OnboardingTask
        {
            EmployeeId = employeeId,
            Title = request.Title,
            Description = request.Description,
            SortOrder = request.SortOrder,
            CreatedBy = actorId,
        };
        _db.OnboardingTasks.Add(task);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "onboarding.task_added", "OnboardingTask", task.Id.ToString(), null, ct);
        return Result<OnboardingTaskDto>.Ok(new OnboardingTaskDto(
            task.Id, task.EmployeeId, $"{employee.FirstName} {employee.LastName}".Trim(), task.Title, task.Description, task.SortOrder, task.IsCompleted, task.CompletedAt));
    }

    public async Task<List<OnboardingTaskDto>> GetMineAsync(Guid employeeId, CancellationToken ct) =>
        await GetTasksForEmployeeAsync(employeeId, ct);

    public async Task<Result<bool>> SetCompletionAsync(Guid taskId, Guid requesterId, bool unscopedAccess, bool isCompleted, CancellationToken ct)
    {
        var task = await _db.OnboardingTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null) return Result<bool>.Fail("NOT_FOUND", "Checklist task not found.");
        if (task.EmployeeId != requesterId && !unscopedAccess)
        {
            return Result<bool>.Fail("FORBIDDEN", "You can only update your own checklist tasks.");
        }

        task.IsCompleted = isCompleted;
        task.CompletedAt = isCompleted ? DateTime.UtcNow : null;
        task.UpdatedBy = requesterId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(requesterId, isCompleted ? "onboarding.task_completed" : "onboarding.task_reopened", "OnboardingTask", task.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteTaskAsync(Guid taskId, Guid actorId, CancellationToken ct)
    {
        var task = await _db.OnboardingTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null) return Result<bool>.Fail("NOT_FOUND", "Checklist task not found.");

        task.IsDeleted = true;
        task.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "onboarding.task_deleted", "OnboardingTask", task.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }
}
