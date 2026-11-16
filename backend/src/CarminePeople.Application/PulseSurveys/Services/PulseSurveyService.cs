using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.PulseSurveys.DTOs;
using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.PulseSurveys.Services;

public class PulseSurveyService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public PulseSurveyService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<PulseSurveyDto> CreateAsync(CreatePulseSurveyRequest request, Guid actorId, CancellationToken ct)
    {
        var survey = new PulseSurvey { Question = request.Question, CreatedBy = actorId };
        _db.PulseSurveys.Add(survey);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "pulse_survey.created", "PulseSurvey", survey.Id.ToString(), null, ct);
        return new PulseSurveyDto(survey.Id, survey.Question, survey.IsActive, 0, null, survey.CreatedAt);
    }

    public async Task<List<PulseSurveyDto>> GetListAsync(CancellationToken ct) =>
        await _db.PulseSurveys.AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new PulseSurveyDto(
                s.Id, s.Question, s.IsActive,
                s.Responses.Count,
                s.Responses.Count == 0 ? null : s.Responses.Average(r => (double)r.Rating),
                s.CreatedAt))
            .ToListAsync(ct);

    public async Task<Result<List<PulseSurveyResponseDto>>> GetResponsesAsync(Guid surveyId, CancellationToken ct)
    {
        if (!await _db.PulseSurveys.AnyAsync(s => s.Id == surveyId, ct))
        {
            return Result<List<PulseSurveyResponseDto>>.Fail("NOT_FOUND", "Survey not found.");
        }

        var responses = await _db.PulseSurveyResponses.AsNoTracking()
            .Where(r => r.SurveyId == surveyId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PulseSurveyResponseDto(
                r.Id, r.IsAnonymous ? null : r.Employee.FirstName + " " + r.Employee.LastName, r.Rating, r.Comment, r.CreatedAt))
            .ToListAsync(ct);

        return Result<List<PulseSurveyResponseDto>>.Ok(responses);
    }

    public async Task<Result<bool>> CloseAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var survey = await _db.PulseSurveys.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (survey is null) return Result<bool>.Fail("NOT_FOUND", "Survey not found.");

        survey.IsActive = false;
        survey.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "pulse_survey.closed", "PulseSurvey", survey.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<List<PulseSurveyDto>> GetPendingForEmployeeAsync(Guid employeeId, CancellationToken ct) =>
        await _db.PulseSurveys.AsNoTracking()
            .Where(s => s.IsActive && !s.Responses.Any(r => r.EmployeeId == employeeId))
            .OrderBy(s => s.CreatedAt)
            .Select(s => new PulseSurveyDto(s.Id, s.Question, s.IsActive, 0, null, s.CreatedAt))
            .ToListAsync(ct);

    public async Task<Result<bool>> SubmitResponseAsync(Guid surveyId, Guid employeeId, SubmitPulseSurveyResponseRequest request, CancellationToken ct)
    {
        var survey = await _db.PulseSurveys.FirstOrDefaultAsync(s => s.Id == surveyId, ct);
        if (survey is null) return Result<bool>.Fail("NOT_FOUND", "Survey not found.");
        if (!survey.IsActive) return Result<bool>.Fail("CLOSED", "This survey is no longer accepting responses.");
        if (await _db.PulseSurveyResponses.AnyAsync(r => r.SurveyId == surveyId && r.EmployeeId == employeeId, ct))
        {
            return Result<bool>.Fail("ALREADY_RESPONDED", "You have already responded to this survey.");
        }

        _db.PulseSurveyResponses.Add(new PulseSurveyResponse
        {
            SurveyId = surveyId,
            EmployeeId = employeeId,
            Rating = request.Rating,
            Comment = request.Comment,
            IsAnonymous = request.IsAnonymous,
            CreatedBy = employeeId,
        });
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(employeeId, "pulse_survey.responded", "PulseSurvey", surveyId.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }
}
