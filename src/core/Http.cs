using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Cadenza;

public static class Http
{
    private static readonly Lazy<HttpClient> _client = new(() => new HttpClient());

    public static HttpClient Client => _client.Value;

    public static async Task<T> GetJson<T>(string url, JsonSerializerContext ctx, CancellationToken ct = default)
    {
        var typeInfo = GetTypeInfo<T>(ctx);
        using var resp = await Client.GetAsync(url, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync(stream, typeInfo, ct).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException($"Deserialization returned null for {typeof(T)}");
    }

    public static async Task<TResp> PostJson<TReq, TResp>(string url, TReq body, JsonSerializerContext ctx, CancellationToken ct = default)
    {
        var reqInfo = GetTypeInfo<TReq>(ctx);
        var respInfo = GetTypeInfo<TResp>(ctx);
        using var content = JsonContent.Create(body, reqInfo);
        using var resp = await Client.PostAsync(url, content, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync(stream, respInfo, ct).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException($"Deserialization returned null for {typeof(TResp)}");
    }

    public static Task<string> GetText(string url, CancellationToken ct = default) =>
        Client.GetStringAsync(url, ct);

    public static async Task Download(string url, string path, CancellationToken ct = default)
    {
        using var resp = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var fs = File.Create(path);
        await resp.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
    }

    private static JsonTypeInfo<T> GetTypeInfo<T>(JsonSerializerContext ctx) =>
        ctx.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
            ?? throw new InvalidOperationException(
                $"JsonSerializerContext {ctx.GetType().Name} does not contain JsonTypeInfo for {typeof(T)}");
}
