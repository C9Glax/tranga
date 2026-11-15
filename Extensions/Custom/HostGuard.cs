using System.Net;
using System.Net.Sockets;

namespace Extensions.Custom;

/// <summary>
/// Stops user-defined sources from making Tranga's server talk to internal services (SSRF): requests to loopback,
/// private, link-local and similar addresses are refused unless explicitly allowed with
/// <c>ALLOW_PRIVATE_SOURCE_HOSTS</c>.
/// </summary>
internal static class HostGuard
{
    /// <summary>True for addresses on the public internet.</summary>
    internal static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.None))
            return false;

        byte[] b = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
            return !(b[0] == 0 // "this" network
                     || b[0] == 10
                     || (b[0] == 100 && b[1] is >= 64 and <= 127) // carrier-grade NAT
                     || (b[0] == 169 && b[1] == 254) // link-local, cloud metadata
                     || (b[0] == 172 && b[1] is >= 16 and <= 31)
                     || (b[0] == 192 && b[1] == 168)
                     || (b[0] == 192 && b[1] == 0 && b[2] == 0)
                     || b[0] >= 224); // multicast + reserved

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast ||
                     (b[0] & 0xFE) == 0xFC); // unique local fc00::/7

        return false;
    }

    /// <summary>Throws when <paramref name="host"/> resolves to anything that is not a public address.</summary>
    internal static async Task EnsureAllowedAsync(string host, bool allowPrivate, CancellationToken ct)
    {
        if (allowPrivate)
            return;

        IPAddress[] addresses = IPAddress.TryParse(host, out IPAddress? literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, ct);
        if (addresses.Length == 0 || addresses.Any(address => !IsPublic(address)))
            throw new HttpRequestException($"Requests to '{host}' are not allowed: it does not resolve to a public address. " +
                                           "Set ALLOW_PRIVATE_SOURCE_HOSTS=true to allow sources on your own network.");
    }

    /// <summary>
    /// A handler that checks the address it is actually connecting to, so redirects and DNS rebinding cannot
    /// sidestep the check.
    /// </summary>
    internal static SocketsHttpHandler CreateHandler(bool allowPrivate) => new()
    {
        AutomaticDecompression = DecompressionMethods.All,
        ConnectCallback = async (context, ct) =>
        {
            await EnsureAllowedAsync(context.DnsEndPoint.Host, allowPrivate, ct);
            IPAddress[] addresses = IPAddress.TryParse(context.DnsEndPoint.Host, out IPAddress? literal)
                ? [literal]
                : await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);

            Socket socket = new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    };

    /// <summary>
    /// Checks the host of every request before handing it on. Used in front of FlareSolverr, which performs the
    /// request itself so the connect-time check above cannot see it.
    /// </summary>
    internal sealed class HostCheckHandler(bool allowPrivate, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri is { } uri)
                await EnsureAllowedAsync(uri.Host, allowPrivate, ct);
            return await base.SendAsync(request, ct);
        }
    }
}
