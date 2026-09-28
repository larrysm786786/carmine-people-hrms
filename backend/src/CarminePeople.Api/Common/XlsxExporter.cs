using System.Reflection;
using ClosedXML.Excel;

namespace CarminePeople.Api.Common;

public static class XlsxExporter
{
    public static byte[] ToXlsx<T>(IEnumerable<T> rows, string sheetName = "Report")
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var col = 0; col < properties.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = properties[col].Name;
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var item in rows)
        {
            for (var col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(item);
                sheet.Cell(row, col + 1).Value = value switch
                {
                    null => string.Empty,
                    DateTime dt => dt.ToString("O"),
                    DateOnly d => d.ToString("yyyy-MM-dd"),
                    _ => value.ToString() ?? string.Empty,
                };
            }
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
