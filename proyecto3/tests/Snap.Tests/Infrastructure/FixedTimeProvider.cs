namespace Snap.Tests.Infrastructure;

/// <summary>Reloj controlado por el test. <see cref="Now"/> se puede mover entre pasos.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
