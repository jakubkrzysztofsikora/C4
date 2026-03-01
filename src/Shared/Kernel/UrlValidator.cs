using System.Net;
using System.Net.Sockets;

namespace C4.Shared.Kernel;

public static class UrlValidator
{
    private static readonly string[] BlockedHostnames =
    [
        "localhost",
        "metadata.google.internal",
        "169.254.169.254"
    ];

    public static Result<Uri> ValidateExternalUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result<Uri>.Failure(new Error("validation.url.empty", "URL cannot be empty."));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return Result<Uri>.Failure(new Error("validation.url.invalid", "URL is not a valid absolute URI."));

        if (uri.Scheme != Uri.UriSchemeHttps)
            return Result<Uri>.Failure(new Error("validation.url.not_https", "Only HTTPS URLs are allowed."));

        if (BlockedHostnames.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            return Result<Uri>.Failure(new Error("validation.url.blocked_host", $"Host '{uri.Host}' is not allowed."));

        if (IPAddress.TryParse(uri.Host, out var ip) && IsPrivateOrReserved(ip))
            return Result<Uri>.Failure(new Error("validation.url.private_ip", "Private or reserved IP addresses are not allowed."));

        return Result<Uri>.Success(uri);
    }

    public static async Task<Result<Uri>> ValidateExternalUrlWithDnsAsync(string url, CancellationToken cancellationToken = default)
    {
        var basicResult = ValidateExternalUrl(url);
        if (basicResult.IsFailure)
            return basicResult;

        var uri = basicResult.Value;

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
            if (addresses.Any(IsPrivateOrReserved))
                return Result<Uri>.Failure(new Error("validation.url.resolves_private", $"Host '{uri.Host}' resolves to a private IP address."));
        }
        catch (SocketException)
        {
            return Result<Uri>.Failure(new Error("validation.url.unresolvable", $"Host '{uri.Host}' could not be resolved."));
        }

        return Result<Uri>.Success(uri);
    }

    private static bool IsPrivateOrReserved(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetwork && bytes.Length == 4)
        {
            return bytes[0] == 127
                || bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254)
                || (bytes[0] == 0);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal;
        }

        return false;
    }
}
