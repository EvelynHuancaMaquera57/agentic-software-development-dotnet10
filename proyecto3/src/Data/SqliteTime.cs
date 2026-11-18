using System.Globalization;

namespace Snap.Data;

/// <summary>
/// Formato único para fechas guardadas en SQLite: ISO 8601 en UTC con 7 decimales y "+00:00"
/// (p. ej. "2026-11-18T16:13:59.4890687+00:00"). Las consultas comparan fechas como texto,
/// así que todas deben escribirse con este formato; un "Z" o un offset distinto rompería el orden.
/// </summary>
public static class SqliteTime
{
    public static string Format(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    public static DateTimeOffset Parse(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
