using System.Globalization;
using System.Reflection;
using System.Text;

namespace CarminePeople.Api.Common;

public static class CsvExporter
{
    public static byte[] ToCsv<T>(IEnumerable<T> rows)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', properties.Select(p => Escape(p.Name))));

        foreach (var row in rows)
        {
            var values = properties.Select(p => Escape(FormatValue(p.GetValue(row))));
            sb.AppendLine(string.Join(',', values));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
