namespace CarminePeople.Infrastructure.Notifications;

// Bound from the "Payroll" config section — the accounts-team mailbox that receives the
// consolidated monthly salary summary so they know how much to transfer to each employee.
public class PayrollOptions
{
    public string? AccountsEmail { get; set; }
}
