using Snap.Data;

namespace Snap.Tests.Data;

/// <summary>Las consultas comparan fechas como texto: el formato tiene que ser siempre idéntico.</summary>
public sealed class SqliteTimeTests
{
    [Fact]
    public void Format_AlwaysWritesUtcWithSevenDecimalsAndZeroOffset()
    {
        var limaTime = new DateTimeOffset(2026, 11, 18, 9, 30, 0, TimeSpan.FromHours(-5));

        Assert.Equal("2026-11-18T14:30:00.0000000+00:00", SqliteTime.Format(limaTime));
    }

    [Fact]
    public void Format_SortsAsTextInChronologicalOrder()
    {
        var earlier = SqliteTime.Format(new DateTimeOffset(2026, 11, 18, 23, 59, 59, TimeSpan.FromHours(-5)));
        var later = SqliteTime.Format(new DateTimeOffset(2026, 11, 19, 4, 59, 59, 999, TimeSpan.Zero));

        Assert.True(string.CompareOrdinal(earlier, later) < 0);
    }

    [Fact]
    public void Parse_RoundTripsFormat()
    {
        var value = new DateTimeOffset(2026, 11, 18, 16, 13, 59, TimeSpan.Zero).AddTicks(4_890_687);

        Assert.Equal(value, SqliteTime.Parse(SqliteTime.Format(value)));
    }
}
