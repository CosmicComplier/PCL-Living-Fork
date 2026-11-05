using System.Net;
using System.Net.Http;
using System.Reflection;
using PCL;

namespace PCLLF.Native.Facade.Tests;
internal static partial class ReviewFixesChecks
{
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        internal readonly List<HttpRequestMessage> Seen = new();
        internal Func<HttpRequestMessage, int, HttpResponseMessage> Respond = null!;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            HttpRequestMessage copy = new(request.Method, request.RequestUri);
            foreach (var header in request.Headers) copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
            if (request.Content is not null) copy.Content = new ByteArrayContent(request.Content.ReadAsByteArrayAsync(token).GetAwaiter().GetResult());
            Seen.Add(copy); return Task.FromResult(Respond(request, Seen.Count));
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (HttpRequestMessage request in Seen) request.Dispose();
            base.Dispose(disposing);
        }
    }
    private static HttpResponseMessage Redirect(string target, HttpStatusCode status = HttpStatusCode.Redirect)
    {
        HttpResponseMessage response = new(status); response.Headers.Location = new Uri(target, UriKind.RelativeOrAbsolute); return response;
    }
    private static void Redirects()
    {
        Type type = Native.Type("PCL.SafeRedirectHandler"); C(146, type.Assembly == Native.Main);
        FieldInfo? runtimeField = typeof(ModNet).GetField("NativeRuntime", Native.Flags); C(148, runtimeField is not null);
        object? runtime = runtimeField!.GetValue(null); C(149, runtime is not null);
        FieldInfo? clientField = runtime!.GetType().GetField("ThreadClient", Native.Flags); C(151, clientField is not null);
        HttpClient? downloadClient = (HttpClient?)clientField!.GetValue(runtime); C(153, downloadClient is not null);
        FieldInfo handlerField = typeof(HttpMessageInvoker).GetFields(Native.Flags).Single(field => field.FieldType == typeof(HttpMessageHandler));
        DelegatingHandler? productionHandler = handlerField.GetValue(downloadClient) as DelegatingHandler;
        C(156, productionHandler is not null && type.IsInstanceOfType(productionHandler));
        C(157, productionHandler!.InnerHandler is HttpClientHandler realTransport && !realTransport.AllowAutoRedirect);
        TimeSpan timeout = downloadClient!.Timeout;
        HttpClient Client(ScriptedHandler transport) => new((HttpMessageHandler)Activator.CreateInstance(type, Native.Flags, null, [transport], null)!);
        // The actual singleton is inspected only. Every Send below is through
        // the real Main redirect handler around a local synthetic handler; no
        // DNS/socket/credential signer or real API transport is invoked.
        var transport = new ScriptedHandler
        {
            Respond = (_, count) => count == 1 ? Redirect("/v1/next") : count == 2 ? Redirect("https://cdn.example.invalid/file") : count == 3 ? Redirect("https://api.curseforge.com/return") : new(HttpStatusCode.OK)
        };
        using (HttpClient client = Client(transport))
        using (HttpRequestMessage request = new(HttpMethod.Get, "https://api.curseforge.com/v1/start"))
        {
            request.Headers.Add("x-api-key", "synthetic-review-key"); request.Headers.Add("Authorization", "Bearer synthetic");
            using (HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult()) C(163, response.StatusCode == HttpStatusCode.OK);
            C(164, transport.Seen.Count == 4);
            C(165, transport.Seen[1].Headers.Contains("x-api-key")); C(166, !transport.Seen[1].Headers.Contains("Authorization"));
            C(167, !transport.Seen[2].Headers.Contains("x-api-key")); C(168, !transport.Seen[3].Headers.Contains("x-api-key"));
        }
        foreach (int status in new[] { 301, 302, 303, 307, 308 })
        {
            var methods = new ScriptedHandler { Respond = (_, count) => count == 1 ? Redirect("/next", (HttpStatusCode)status) : new(HttpStatusCode.OK) };
            using HttpClient client = Client(methods);
            using HttpResponseMessage response = client.PostAsync("https://example.invalid/start", new StringContent("fixture-body")).GetAwaiter().GetResult();
            C(174, methods.Seen[1].Method == (status is 307 or 308 ? HttpMethod.Post : HttpMethod.Get), status.ToString());
            if (status is 307 or 308) C(175, methods.Seen[1].Content!.ReadAsStringAsync().GetAwaiter().GetResult() == "fixture-body", status.ToString());
            else C(176, methods.Seen[1].Content is null, status.ToString());
        }
        foreach (string target in new[] { "http://api.curseforge.com/v1/next", "file:///C:/unexpected" })
        {
            var blocked = new ScriptedHandler { Respond = (_, _) => Redirect(target) };
            using HttpClient client = Client(blocked); bool failed = false;
            try { using HttpResponseMessage response = client.GetAsync("https://api.curseforge.com/start").GetAwaiter().GetResult(); }
            catch (HttpRequestException) { failed = true; }
            C(184, failed && blocked.Seen.Count == 1, target);
        }
        var loop = new ScriptedHandler { Respond = (_, _) => Redirect("/loop") };
        using (HttpClient client = Client(loop))
        {
            bool failed = false; try { using HttpResponseMessage response = client.GetAsync("https://example.invalid/loop").GetAwaiter().GetResult(); } catch (HttpRequestException) { failed = true; }
            C(190, failed && loop.Seen.Count <= 11);
        }
        Native.Check(ReferenceEquals(handlerField.GetValue(downloadClient), productionHandler) && downloadClient.Timeout == timeout &&
            productionHandler.InnerHandler is HttpClientHandler same && !same.AllowAutoRedirect, "Redirect fixtures mutated/disposed the actual Main singleton transport.");
        Console.WriteLine("PASS review Redirects: real Main download handler wiring, same-host/cross-host credential removal, 301/302/303/307/308 body/method rules, unsafe targets and bounded loops via synthetic handlers only.");
    }
}
