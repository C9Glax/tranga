namespace Common.Services.Authentication;

/// <summary>
/// Computes escalating login lockout durations for the single admin account. Deliberately account-scoped
/// (there is no per-IP tracking) - see <c>Services.Auth.Features.Auth.PostLoginEndpoint</c> for how this is
/// applied.
/// </summary>
public static class LoginLockoutPolicy
{
    /// <summary>Number of consecutive failed attempts before a lockout starts.</summary>
    public const int Threshold = 3;

    private const double BaseSeconds = 10;
    private const double GrowthFactor = 1.5;
    private static readonly TimeSpan MaxLockout = TimeSpan.FromDays(1);

    /// <summary>
    /// The lockout duration for <paramref name="failedAttempts"/> consecutive failures, or <c>null</c> if that's
    /// still below <see cref="Threshold"/>. Grows as <c>10s * 1.5^(failedAttempts - Threshold)</c>, capped at 1 day.
    /// </summary>
    public static TimeSpan? ComputeLockout(int failedAttempts)
    {
        if (failedAttempts < Threshold)
            return null;

        double seconds = BaseSeconds * Math.Pow(GrowthFactor, failedAttempts - Threshold);
        return TimeSpan.FromSeconds(Math.Min(seconds, MaxLockout.TotalSeconds));
    }

    /// <summary>Formats a lockout duration for display, e.g. <c>"10s"</c>, <c>"1m 20s"</c>, <c>"3h 4m"</c>, <c>"1 day"</c>.</summary>
    public static string Format(TimeSpan duration)
    {
        if (duration >= TimeSpan.FromDays(1))
            return "1 day";

        if (duration >= TimeSpan.FromHours(1))
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";

        if (duration >= TimeSpan.FromMinutes(1))
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";

        return $"{Math.Max((int)Math.Ceiling(duration.TotalSeconds), 1)}s";
    }
}
