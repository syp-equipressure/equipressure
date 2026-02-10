using NodaTime.Extensions;

namespace Library.Core;

public interface IDateTimeProvider
{
    public LocalDate GetCurrentDate();
    public Instant GetCurrentInstant();
    public Instant ConvertDateToInstant(LocalDate date);
}

public sealed class DateTimeProvider(IClock clock) : IDateTimeProvider
{
    public static readonly DateTimeZone TimeZone = DateTimeZoneProviders.Tzdb["Europe/Vienna"];

    public LocalDate GetCurrentDate() => clock.InZone(TimeZone).GetCurrentDate();

    public Instant GetCurrentInstant() => clock.InZone(TimeZone).GetCurrentInstant();

    public Instant ConvertDateToInstant(LocalDate date) => date.AtStartOfDayInZone(TimeZone).ToInstant();
}
