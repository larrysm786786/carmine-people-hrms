using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

// Singleton row (like BrandingSettings) — admin-configured statutory rates used by the
// payroll compliance calculator. Deliberately starts with every rate at 0/disabled; HR
// must enter their own verified figures rather than trust a hardcoded guess at tax law.
public class PayrollComplianceSettings : AuditableEntity
{
    public bool IsPfEnabled { get; set; }
    public decimal PfEmployeeRatePercent { get; set; }
    public decimal PfEmployerRatePercent { get; set; }
    public decimal? PfWageCeiling { get; set; }

    public bool IsEsiEnabled { get; set; }
    public decimal EsiEmployeeRatePercent { get; set; }
    public decimal EsiEmployerRatePercent { get; set; }
    public decimal? EsiEligibilityThreshold { get; set; }

    public bool IsTdsEnabled { get; set; }
}
