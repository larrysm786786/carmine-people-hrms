using CarminePeople.Domain.Common;

namespace CarminePeople.Domain.Entities;

// One progressive annual-income tax bracket. Null UpToAnnualIncome marks the
// top/unbounded bracket. Admin-entered — no hardcoded slab defaults.
public class TdsSlab : AuditableEntity
{
    public decimal? UpToAnnualIncome { get; set; }
    public decimal RatePercent { get; set; }
    public int SortOrder { get; set; }
}
