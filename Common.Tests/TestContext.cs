namespace Common.Tests;

public abstract class TestContext
{
    // ReSharper disable once InconsistentNaming
    protected CancellationToken ct { get; init; } = Xunit.TestContext.Current.CancellationToken;
}