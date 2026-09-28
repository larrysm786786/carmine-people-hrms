namespace CarminePeople.Application.Attendance.Services;

public static class WorkingMinutesCalculator
{
    // Gross elapsed time minus all completed breaks. Correctly handles overnight/night
    // shifts because it operates on absolute DateTime instants, not time-of-day components.
    public static int Calculate(DateTime checkInAt, DateTime checkOutAt, int totalBreakMinutes)
    {
        var grossMinutes = (int)(checkOutAt - checkInAt).TotalMinutes;
        return Math.Max(0, grossMinutes - totalBreakMinutes);
    }
}
