namespace CarminePeople.Domain.Enums;

// How this attendance record's check-in was verified. Only Gps is used today;
// Qr and Manual exist so a future QR-based attendance feature (see roadmap
// "Future QR Attendance") can be added by extending this enum and CheckInRequest,
// without touching the existing GPS check-in flow or requiring a schema migration
// beyond the one that introduces this field.
public enum CheckInMethod
{
    Gps,
    Qr,
    Manual,
    Face
}
