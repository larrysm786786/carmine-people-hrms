using CarminePeople.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CarminePeople.Infrastructure.Reports;

// One day's attendance for the grid report — a day with no AttendanceRecord at all (e.g. a
// future date within the month, or a gap) renders as blank rather than a fabricated status.
public record AttendanceGridDay(DateOnly Date, string? StatusCode, TimeOnly? CheckInAt, TimeOnly? CheckOutAt, int WorkingMinutes);

public record AttendanceGridReportData(
    string CompanyName, string EmployeeCode, string EmployeeName, string DepartmentName,
    DateOnly MonthStart, DateOnly MonthEnd, List<AttendanceGridDay> Days);

// Renders a per-employee monthly attendance grid PDF matching the classic biometric-machine
// "Monthly Status Report" layout: one column per calendar day, with Status/In-Time/Out-Time/
// Total rows stacked underneath — the format requested to match the company's existing eSSL
// report exports, so HR/employees see a familiar layout rather than a different one from us.
public static class AttendanceGridPdfGenerator
{
    static AttendanceGridPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(AttendanceGridReportData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(7));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("Monthly Status Report (Basic Work Duration)").FontSize(14).Bold();
                    col.Item().AlignCenter().Text($"{data.MonthStart:MMM dd yyyy} To {data.MonthEnd:MMM dd yyyy}").FontSize(9);
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text($"Company: {data.CompanyName}").Bold();
                        row.RelativeItem().AlignRight().Text($"Printed On: {DateTime.Now:MMM dd yyyy HH:mm}");
                    });
                    col.Item().PaddingTop(4).Text($"Emp. Code: {data.EmployeeCode}   Emp. Name: {data.EmployeeName}   Department: {data.DepartmentName}").Bold();
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    // One narrow column per calendar day, plus a fixed label column on the left.
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(50);
                        foreach (var _ in data.Days) columns.RelativeColumn();
                    });

                    void HeaderCell(string text) => table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(text).Bold();
                    void LabelCell(string text) => table.Cell().Border(0.5f).Padding(2).Background(Colors.Grey.Lighten3).Text(text).Bold();
                    void ValueCell(string text) => table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(text);

                    // Day-number + weekday-initial header row (e.g. "1 Mo", "2 Tu"...).
                    HeaderCell("Days");
                    foreach (var day in data.Days) HeaderCell($"{day.Date.Day} {day.Date.DayOfWeek.ToString()[..2]}");

                    LabelCell("Status");
                    foreach (var day in data.Days) ValueCell(day.StatusCode ?? "-");

                    LabelCell("In-Time");
                    foreach (var day in data.Days) ValueCell(day.CheckInAt?.ToString("HH:mm") ?? "-");

                    LabelCell("Out-Time");
                    foreach (var day in data.Days) ValueCell(day.CheckOutAt?.ToString("HH:mm") ?? "-");

                    LabelCell("Total");
                    foreach (var day in data.Days) ValueCell(FormatDuration(day.WorkingMinutes));
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatDuration(int minutes)
    {
        if (minutes <= 0) return "00:00";
        return $"{minutes / 60}:{minutes % 60:D2}";
    }
}

// Maps AttendanceStatus to the short codes used on the classic biometric-machine report
// (P=Present, A=Absent, WO=Week Off, SL=Sick Leave/Leave, HD=Half Day, H=Holiday) so the
// PDF reads exactly like the eSSL export employees are already familiar with.
public static class AttendanceStatusCode
{
    public static string For(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => "P",
        AttendanceStatus.Absent => "A",
        AttendanceStatus.Late => "P",
        AttendanceStatus.HalfDay => "HD",
        AttendanceStatus.Leave => "SL",
        AttendanceStatus.Holiday => "H",
        AttendanceStatus.WeekOff => "WO",
        AttendanceStatus.WFH => "WFH",
        AttendanceStatus.PendingRegularization => "P",
        _ => "-",
    };
}
