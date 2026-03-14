namespace Common.Helpers;

public abstract class TestContext : IAsyncDisposable
{
    private readonly CancellationTokenSource cts = new ();

    protected CancellationToken ct { get; init; }

    protected TestContext()
    {
        ct = cts.Token;
    }

    public async ValueTask DisposeAsync()
    {
        await cts.CancelAsync();
        cts.Dispose();
    }
}