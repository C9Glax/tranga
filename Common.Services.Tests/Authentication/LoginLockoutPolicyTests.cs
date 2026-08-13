using Common.Services.Authentication;
using Common.Tests;

namespace Common.Services.Tests.Authentication;

public class LoginLockoutPolicyTests : TrangaTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ComputeLockout_BelowThreshold_ReturnsNull(int failedAttempts)
    {
        Assert.Null(LoginLockoutPolicy.ComputeLockout(failedAttempts));
    }

    [Theory]
    [InlineData(3, 10)]
    [InlineData(4, 15)]
    [InlineData(5, 22.5)]
    public void ComputeLockout_AtOrAboveThreshold_GrowsByFactorOfOnePointFive(int failedAttempts, double expectedSeconds)
    {
        TimeSpan? lockout = LoginLockoutPolicy.ComputeLockout(failedAttempts);

        Assert.NotNull(lockout);
        Assert.Equal(expectedSeconds, lockout.Value.TotalSeconds, precision: 3);
    }

    [Fact]
    public void ComputeLockout_IsCappedAtOneDay()
    {
        TimeSpan? lockout = LoginLockoutPolicy.ComputeLockout(failedAttempts: 30);

        Assert.Equal(TimeSpan.FromDays(1), lockout);
    }

    [Fact]
    public void ComputeLockout_NeverExceedsOneDay_EvenForVeryLargeAttemptCounts()
    {
        TimeSpan? lockout = LoginLockoutPolicy.ComputeLockout(failedAttempts: 1000);

        Assert.Equal(TimeSpan.FromDays(1), lockout);
    }

    [Theory]
    [InlineData(1, "1s")]
    [InlineData(10, "10s")]
    [InlineData(59, "59s")]
    [InlineData(60, "1m 0s")]
    [InlineData(80, "1m 20s")]
    [InlineData(3600, "1h 0m")]
    [InlineData(11040, "3h 4m")]
    [InlineData(86400, "1 day")]
    [InlineData(200000, "1 day")]
    public void Format_ProducesHumanReadableDuration(int seconds, string expected)
    {
        Assert.Equal(expected, LoginLockoutPolicy.Format(TimeSpan.FromSeconds(seconds)));
    }
}
