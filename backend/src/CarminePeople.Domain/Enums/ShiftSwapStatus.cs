namespace CarminePeople.Domain.Enums;

// A swap needs the counterpart employee to accept before it can even reach a
// manager — RequestedAcceptance is the state while waiting on that peer
// acceptance, distinct from Pending (waiting on manager decision after both
// employees have agreed).
public enum ShiftSwapStatus
{
    RequestedAcceptance,
    Pending,
    Approved,
    Rejected,
    Cancelled
}
