using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Payroll.DTOs;
using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Payroll.Services;

public class PayrollComplianceService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public PayrollComplianceService(IApplicationDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task<PayrollComplianceSettingsDto> GetSettingsAsync(CancellationToken ct)
    {
        var settings = await _db.PayrollComplianceSettings.FirstOrDefaultAsync(ct);
        return settings is null ? DefaultSettingsDto() : ToDto(settings);
    }

    public async Task<PayrollComplianceSettingsDto> UpdateSettingsAsync(UpdatePayrollComplianceSettingsRequest request, Guid actorId, CancellationToken ct)
    {
        var settings = await _db.PayrollComplianceSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new PayrollComplianceSettings { CreatedBy = actorId };
            _db.PayrollComplianceSettings.Add(settings);
        }

        settings.IsPfEnabled = request.IsPfEnabled;
        settings.PfEmployeeRatePercent = request.PfEmployeeRatePercent;
        settings.PfEmployerRatePercent = request.PfEmployerRatePercent;
        settings.PfWageCeiling = request.PfWageCeiling;
        settings.IsEsiEnabled = request.IsEsiEnabled;
        settings.EsiEmployeeRatePercent = request.EsiEmployeeRatePercent;
        settings.EsiEmployerRatePercent = request.EsiEmployerRatePercent;
        settings.EsiEligibilityThreshold = request.EsiEligibilityThreshold;
        settings.IsTdsEnabled = request.IsTdsEnabled;
        settings.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "payroll_compliance.settings_updated", "PayrollComplianceSettings", settings.Id.ToString(), null, ct);
        return ToDto(settings);
    }

    public async Task<List<TdsSlabDto>> GetTdsSlabsAsync(CancellationToken ct) =>
        await _db.TdsSlabs.AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new TdsSlabDto(s.Id, s.UpToAnnualIncome, s.RatePercent, s.SortOrder))
            .ToListAsync(ct);

    public async Task<TdsSlabDto> AddTdsSlabAsync(UpsertTdsSlabRequest request, Guid actorId, CancellationToken ct)
    {
        var slab = new TdsSlab { UpToAnnualIncome = request.UpToAnnualIncome, RatePercent = request.RatePercent, SortOrder = request.SortOrder, CreatedBy = actorId };
        _db.TdsSlabs.Add(slab);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "payroll_compliance.tds_slab_added", "TdsSlab", slab.Id.ToString(), null, ct);
        return new TdsSlabDto(slab.Id, slab.UpToAnnualIncome, slab.RatePercent, slab.SortOrder);
    }

    public async Task<Result<TdsSlabDto>> UpdateTdsSlabAsync(Guid id, UpsertTdsSlabRequest request, Guid actorId, CancellationToken ct)
    {
        var slab = await _db.TdsSlabs.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (slab is null) return Result<TdsSlabDto>.Fail("NOT_FOUND", "TDS slab not found.");

        slab.UpToAnnualIncome = request.UpToAnnualIncome;
        slab.RatePercent = request.RatePercent;
        slab.SortOrder = request.SortOrder;
        slab.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "payroll_compliance.tds_slab_updated", "TdsSlab", slab.Id.ToString(), null, ct);
        return Result<TdsSlabDto>.Ok(new TdsSlabDto(slab.Id, slab.UpToAnnualIncome, slab.RatePercent, slab.SortOrder));
    }

    public async Task<Result<bool>> DeleteTdsSlabAsync(Guid id, Guid actorId, CancellationToken ct)
    {
        var slab = await _db.TdsSlabs.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (slab is null) return Result<bool>.Fail("NOT_FOUND", "TDS slab not found.");

        slab.IsDeleted = true;
        slab.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "payroll_compliance.tds_slab_deleted", "TdsSlab", slab.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<ComplianceCalculationDto> CalculateAsync(CalculateComplianceRequest request, CancellationToken ct)
    {
        var settings = await _db.PayrollComplianceSettings.FirstOrDefaultAsync(ct);
        var gross = request.MonthlyGrossAmount;

        decimal pfEmployee = 0, pfEmployer = 0, esiEmployee = 0, esiEmployer = 0;

        if (settings is { IsPfEnabled: true })
        {
            var pfBase = settings.PfWageCeiling.HasValue ? Math.Min(gross, settings.PfWageCeiling.Value) : gross;
            pfEmployee = Math.Round(pfBase * settings.PfEmployeeRatePercent / 100, 2);
            pfEmployer = Math.Round(pfBase * settings.PfEmployerRatePercent / 100, 2);
        }

        if (settings is { IsEsiEnabled: true } && (!settings.EsiEligibilityThreshold.HasValue || gross <= settings.EsiEligibilityThreshold.Value))
        {
            esiEmployee = Math.Round(gross * settings.EsiEmployeeRatePercent / 100, 2);
            esiEmployer = Math.Round(gross * settings.EsiEmployerRatePercent / 100, 2);
        }

        decimal tds = 0;
        if (settings is { IsTdsEnabled: true })
        {
            var slabs = await _db.TdsSlabs.AsNoTracking().OrderBy(s => s.SortOrder).ToListAsync(ct);
            tds = Math.Round(CalculateAnnualTax(gross * 12, slabs) / 12, 2);
        }

        return new ComplianceCalculationDto(pfEmployee, pfEmployer, esiEmployee, esiEmployer, tds, pfEmployee + esiEmployee + tds);
    }

    // Standard progressive-bracket computation: each slab taxes only the portion of income
    // that falls within it, not the whole income at that slab's rate.
    private static decimal CalculateAnnualTax(decimal annualIncome, List<TdsSlab> slabs)
    {
        decimal totalTax = 0;
        decimal previousThreshold = 0;

        foreach (var slab in slabs)
        {
            var bracketTop = slab.UpToAnnualIncome ?? decimal.MaxValue;
            var taxableInBracket = Math.Max(0, Math.Min(annualIncome, bracketTop) - previousThreshold);
            totalTax += taxableInBracket * slab.RatePercent / 100;
            previousThreshold = bracketTop;
            if (annualIncome <= bracketTop) break;
        }

        return totalTax;
    }

    private static PayrollComplianceSettingsDto ToDto(PayrollComplianceSettings s) => new(
        s.IsPfEnabled, s.PfEmployeeRatePercent, s.PfEmployerRatePercent, s.PfWageCeiling,
        s.IsEsiEnabled, s.EsiEmployeeRatePercent, s.EsiEmployerRatePercent, s.EsiEligibilityThreshold,
        s.IsTdsEnabled);

    private static PayrollComplianceSettingsDto DefaultSettingsDto() => new(false, 0, 0, null, false, 0, 0, null, false);
}
