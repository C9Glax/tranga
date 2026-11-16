using System.Reflection;
using Common.Services.Events.Events;
using Common.Tests;
using Extensions;
using Moq;
using RabbitMQ.Client;
using Services.Tasks.EventHandlers;

namespace Services.Tasks.Tests.EventHandlers;

public sealed class DownloadExtensionsChangedHandlerTests : TrangaTest
{
    private sealed class CountingProvider : IDownloadExtensionProvider
    {
        public int Loads;
        public string Name => "Counting";

        public Task<IReadOnlyList<IDownloadExtension>> LoadAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref Loads);
            return Task.FromResult<IReadOnlyList<IDownloadExtension>>([]);
        }
    }

    private static async Task<bool> InvokeHandleMessage(DownloadExtensionsChangedHandler handler)
    {
        MethodInfo method = Assert.Single(typeof(DownloadExtensionsChangedHandler)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(m => m.Name == "HandleMessage"));
        return await Assert.IsAssignableFrom<Task<bool>>(method.Invoke(handler, [new DownloadExtensionsChangedEvent()]));
    }

    [Fact]
    public async Task HandleMessage_ReloadsTheDownloadExtensionsAndAcknowledges()
    {
        CountingProvider provider = new();
        DownloadExtensionsCollection.RegisterProvider(provider);
        DownloadExtensionsChangedHandler handler = new(new Mock<IChannel>().Object);

        bool handled = await InvokeHandleMessage(handler);

        Assert.True(handled);
        Assert.Equal(1, provider.Loads);
    }
}
