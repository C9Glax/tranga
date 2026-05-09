namespace Common.Tests;

public abstract class TrangaTest
{
    // ReSharper disable once InconsistentNaming
    protected CancellationToken ct { get; init; } = Xunit.TestContext.Current.CancellationToken;
}