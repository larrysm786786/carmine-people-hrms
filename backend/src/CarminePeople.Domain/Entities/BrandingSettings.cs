using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

// Single-row table holding the currently active branding. Seeded once; updated via the branding API.
public class BrandingSettings : AuditableEntity
{
    public string CompanyName { get; set; } = "Carmine People";
    public string AppName { get; set; } = "Carmine People";
    public string? LogoUrl { get; set; }
    public string ColorGray { get; set; } = "#f0f0f0";
    public string ColorPrimary { get; set; } = "#cd0040";
    public string ColorBlack { get; set; } = "#000000";
    public string ColorWhite { get; set; } = "#ffffff";
    public string GradientCss { get; set; } = "linear-gradient(90deg, #cd0040 0%, #3a1420 52%, #000000 100%)";

    // IANA time zone ID (e.g. "Asia/Kolkata"). All timestamps are stored in UTC everywhere
    // in the backend/database; this is the single company-wide setting used to convert them
    // for display — never a per-request override, since attendance/leave dates must mean
    // the same calendar day for everyone at the company regardless of the viewing client.
    public string TimeZoneId { get; set; } = "UTC";

    // Company-wide late-arrival policy: within a calendar month, this many late check-ins are
    // a free warning, the next one is a half-day pay cut, then the cycle repeats for the rest
    // of the month — see AttendanceLatePolicyCalculator.
    public int FreeLatesBeforeHalfDay { get; set; } = 2;
}
