using System.Net;
using Extensions.Custom;

namespace Extensions.Tests.Custom;

public sealed class HostGuardTests : Common.Tests.TrangaTest
{
    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("93.184.216.34")]
    [InlineData("172.15.0.1")] // just outside 172.16/12
    [InlineData("172.32.0.1")]
    [InlineData("100.63.255.255")] // just outside 100.64/10
    [InlineData("100.128.0.1")]
    [InlineData("2606:4700:4700::1111")]
    public void PublicAddressesAreAllowed(string address) => Assert.True(HostGuard.IsPublic(IPAddress.Parse(address)));

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.8.8.8")]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")] // cloud metadata
    [InlineData("100.64.0.1")] // carrier-grade NAT
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("ff02::1")]
    [InlineData("::ffff:127.0.0.1")] // IPv4-mapped loopback
    [InlineData("::ffff:10.0.0.1")]
    public void PrivateAddressesAreBlocked(string address) => Assert.False(HostGuard.IsPublic(IPAddress.Parse(address)));

    [Fact]
    public async Task LoopbackHostIsRefused()
    {
        HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(() => HostGuard.EnsureAllowedAsync("localhost", allowPrivate: false, ct));
        Assert.Contains("ALLOW_PRIVATE_SOURCE_HOSTS", e.Message);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.0.10")]
    [InlineData("::1")]
    public async Task PrivateLiteralsAreRefused(string host) =>
        await Assert.ThrowsAsync<HttpRequestException>(() => HostGuard.EnsureAllowedAsync(host, allowPrivate: false, ct));

    [Fact]
    public async Task PublicLiteralIsAccepted() => await HostGuard.EnsureAllowedAsync("8.8.8.8", allowPrivate: false, ct);

    [Fact]
    public async Task EverythingIsAcceptedWhenPrivateHostsAreAllowed()
    {
        await HostGuard.EnsureAllowedAsync("localhost", allowPrivate: true, ct);
        await HostGuard.EnsureAllowedAsync("10.1.2.3", allowPrivate: true, ct);
    }
}
