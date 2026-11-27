using System;
using FlightCareer.Domain.Connection;
using Xunit;

namespace FlightCareer.Tests.Connection;

public class SimLinkWatchdogTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5000)]
    public void Constructor_NonPositiveTimeout_Throws(int milliseconds)
    {
        var timeout = TimeSpan.FromMilliseconds(milliseconds);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimLinkWatchdog(timeout));
    }

    [Fact]
    public void NotArmed_NeverStale()
    {
        var watchdog = new SimLinkWatchdog(TimeSpan.FromSeconds(30));
        var t = new DateTime(2026, 11, 27, 10, 0, 0, DateTimeKind.Utc);

        Assert.False(watchdog.IsArmed);
        Assert.False(watchdog.IsStale(t + TimeSpan.FromHours(1)));
        Assert.Equal(TimeSpan.Zero, watchdog.SilenceFor(t + TimeSpan.FromHours(1)));
    }

    [Fact]
    public void Connected_WithinTimeout_NotStale()
    {
        var watchdog = new SimLinkWatchdog(TimeSpan.FromSeconds(30));
        var t = new DateTime(2026, 11, 27, 10, 0, 0, DateTimeKind.Utc);

        watchdog.MarkConnected(t);

        Assert.True(watchdog.IsArmed);
        // Limite exato não é estritamente maior que SilenceTimeout
        Assert.False(watchdog.IsStale(t + TimeSpan.FromSeconds(30)));
        Assert.Equal(TimeSpan.FromSeconds(30), watchdog.SilenceFor(t + TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void Connected_PastTimeout_Stale()
    {
        var watchdog = new SimLinkWatchdog(TimeSpan.FromSeconds(30));
        var t = new DateTime(2026, 11, 27, 10, 0, 0, DateTimeKind.Utc);

        watchdog.MarkConnected(t);

        var staleTime = t + TimeSpan.FromMilliseconds(30001);
        Assert.True(watchdog.IsStale(staleTime));
        Assert.Equal(TimeSpan.FromMilliseconds(30001), watchdog.SilenceFor(staleTime));
    }

    [Fact]
    public void Message_ResetsClock()
    {
        var watchdog = new SimLinkWatchdog(TimeSpan.FromSeconds(30));
        var t = new DateTime(2026, 11, 27, 10, 0, 0, DateTimeKind.Utc);

        watchdog.MarkConnected(t);
        watchdog.MarkMessage(t + TimeSpan.FromSeconds(25));

        Assert.False(watchdog.IsStale(t + TimeSpan.FromSeconds(50))); // 25s de silêncio <= 30s
        Assert.True(watchdog.IsStale(t + TimeSpan.FromSeconds(56)));  // 31s de silêncio > 30s
    }

    [Fact]
    public void Disconnected_DisarmsAndIgnoresMessages()
    {
        var watchdog = new SimLinkWatchdog(TimeSpan.FromSeconds(30));
        var t = new DateTime(2026, 11, 27, 10, 0, 0, DateTimeKind.Utc);

        watchdog.MarkConnected(t);
        watchdog.MarkDisconnected();
        watchdog.MarkMessage(t + TimeSpan.FromSeconds(1));

        Assert.False(watchdog.IsArmed);
        Assert.False(watchdog.IsStale(t + TimeSpan.FromHours(1)));
        Assert.Equal(TimeSpan.Zero, watchdog.SilenceFor(t + TimeSpan.FromHours(1)));
    }

    [Fact]
    public void Reconnect_RearmsFromNewTime()
    {
        var watchdog = new SimLinkWatchdog(TimeSpan.FromSeconds(30));
        var t = new DateTime(2026, 11, 27, 10, 0, 0, DateTimeKind.Utc);

        watchdog.MarkConnected(t);
        watchdog.MarkDisconnected();
        watchdog.MarkConnected(t + TimeSpan.FromSeconds(100));

        Assert.True(watchdog.IsArmed);
        Assert.False(watchdog.IsStale(t + TimeSpan.FromSeconds(120))); // 20s <= 30s
        Assert.True(watchdog.IsStale(t + TimeSpan.FromSeconds(131)));  // 31s > 30s
    }
}
