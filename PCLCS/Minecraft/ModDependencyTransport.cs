using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PCLCS;

/// <summary>The LF exact-host signing boundary remains supplied by the launcher; transport behavior is C#.</summary>
public static class ModDependencyTransport {
    public static bool IsAllowedApi(string? url) {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed)) return false;
        Uri address = parsed!;
        if (address.Scheme != "https" || !address.IsDefaultPort || address.UserInfo != "") return false;
        return (address.Host.Equals("api.modrinth.com", StringComparison.OrdinalIgnoreCase) && address.AbsolutePath.StartsWith("/v2/", StringComparison.Ordinal)) ||
            (address.Host.Equals("api.curseforge.com", StringComparison.OrdinalIgnoreCase) && address.AbsolutePath.StartsWith("/v1/", StringComparison.Ordinal));
    }
    public static JToken DefaultRequest(string url, HttpMethod method, string? body, CancellationToken token,
        Action<string, HttpRequestMessage> signRequest, Func<int, int, ModDependencyHttpFailure> httpFailure) {
        if (!IsAllowedApi(url)) throw new ArgumentException("Unsupported dependency API address.");
        // Original signed headers never follow redirects; signing does not replace the request being sent.
        using HttpClientHandler handler = new() { AllowAutoRedirect = false, UseCookies = false, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
        using HttpClient client = new(handler);
        using HttpRequestMessage request = new(method, url);
        client.Timeout = Timeout.InfiniteTimeSpan;
        if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        signRequest(url, request);
        using HttpResponseMessage response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode) {
            int retrySeconds = 60;
            if (response.Headers.RetryAfter?.Delta.HasValue == true)
                retrySeconds = Convert.ToInt32(Math.Min(900, response.Headers.RetryAfter!.Delta!.Value.TotalSeconds));
            else if (response.Headers.RetryAfter?.Date.HasValue == true)
                retrySeconds = Convert.ToInt32(Math.Min(900, (response.Headers.RetryAfter!.Date!.Value - DateTimeOffset.UtcNow).TotalSeconds));
            throw httpFailure((int)response.StatusCode, retrySeconds);
        }
        using Stream input = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using MemoryStream buffer = new();
        byte[] bytes = new byte[8192];
        while (true) {
            int count = input.ReadAsync(bytes, 0, bytes.Length, token).GetAwaiter().GetResult();
            if (count == 0) break;
            if (buffer.Length + count > 4L * 1024 * 1024) throw new InvalidDataException("Dependency API response exceeds limit.");
            buffer.Write(bytes, 0, count);
        }
        token.ThrowIfCancellationRequested();
        return JToken.Parse(new UTF8Encoding(false, true).GetString(buffer.ToArray()));
    }
}
