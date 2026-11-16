using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

// Deliberately simple: a single gross/deductions/net record per employee per
// month, entered by HR — not a full payroll engine with tax slabs, statutory
// components, or automated computation. That is a separate, much larger
// project; this covers "employee can see what they were paid."
public class Payslip : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int Year { get; set; }
    public int Month { get; set; }

    public decimal GrossAmount { get; set; }
    public decimal DeductionsAmount { get; set; }
    public decimal NetAmount { get; set; }

    // Optional statutory breakdown — populated only when HR used the compliance calculator
    // to arrive at DeductionsAmount; null means "not broken down" (plain manual entry).
    public decimal? PfEmployeeAmount { get; set; }
    public decimal? PfEmployerAmount { get; set; }
    public decimal? EsiEmployeeAmount { get; set; }
    public decimal? EsiEmployerAmount { get; set; }
    public decimal? TdsAmount { get; set; }

    public string? Notes { get; set; }
}
