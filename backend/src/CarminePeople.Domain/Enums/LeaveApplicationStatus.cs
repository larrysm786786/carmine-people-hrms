namespace CarminePeople.Domain.Enums;

public enum LeaveApplicationStatus
{
    Pending,
    // A Manager's approval is not final — it moves the request to ManagerApproved,
    // awaiting a second HR decision, mirroring the two-step chain already used for
    // attendance regularization (see RegularizationStatus). HR/SuperAdmin approval
    // (from either Pending or ManagerApproved) finalizes it as Approved.
    ManagerApproved,
    Approved,
    Rejected,
    Cancelled
}
