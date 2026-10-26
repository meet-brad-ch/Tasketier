namespace Tasketier.Core.Tests;

/// <summary>A clock the tests set by hand. Local time equals UTC so dates are stable.</summary>
public sealed class FakeClock : TimeProvider
{
    public FakeClock(DateTimeOffset now)
    {
        Now = now;
    }

    public DateTimeOffset Now { get; set; }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();

    public void Advance(TimeSpan by) => Now += by;
}
