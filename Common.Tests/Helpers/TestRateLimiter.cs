using System.Threading.RateLimiting;

namespace Common.Tests.Helpers;

/// <summary>
/// A controllable RateLimiter test double: acquisition either always succeeds or always
/// fails, and every AcquireAsync/Dispose call is recorded for assertions.
/// </summary>
internal sealed class TestRateLimiter(bool acquireSucceeds = true) : RateLimiter
{
    public int AcquireAsyncCallCount { get; private set; }
    public bool Disposed { get; private set; }

    public override TimeSpan? IdleDuration => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount) => new TestLease(acquireSucceeds);

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        AcquireAsyncCallCount++;
        return ValueTask.FromResult<RateLimitLease>(new TestLease(acquireSucceeds));
    }

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }

    private sealed class TestLease(bool isAcquired) : RateLimitLease
    {
        public override bool IsAcquired => isAcquired;
        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}