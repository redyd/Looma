namespace Looma.Domain.Extensions;

public static class DateExtensions
{
    public static DateOnly? ToDateOnly(this DateTimeOffset? value) =>
        value is null ? null : DateOnly.FromDateTime(value.Value.DateTime);

    public static DateTimeOffset? ToDateTimeOffset(this DateOnly? value) =>
        value is null ? null : new DateTimeOffset(value.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}