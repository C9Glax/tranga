using Microsoft.Extensions.DependencyInjection;

namespace Services.Tasks.Tests.Helpers;

/// <summary>
/// An <see cref="IServiceScope"/> that resolves nothing, for Tasks whose <c>RefreshScope</c> doesn't need any service.
/// </summary>
internal sealed class NoOpServiceScope : IServiceScope, IServiceProvider
{
    public IServiceProvider ServiceProvider => this;

    public object? GetService(Type serviceType) => null;

    public void Dispose()
    {
    }
}
