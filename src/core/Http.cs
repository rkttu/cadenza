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

/// <summary>
/// HTTP helpers backed by a shared singleton <see cref="HttpClient"/>.
/// JSON methods take an explicit <see cref="JsonSerializerContext"/> so that
/// scripts remain AOT-clean: nothing in this module uses reflection-based
/// serialization.
/// </summary>
public static class Http
{
    private static readonly Lazy<HttpClient> _client = new(() => new HttpClient());

    /// <summary>
    /// The process-wide shared <see cref="HttpClient"/>. Use it directly when
    /// you need headers, custom timeouts, streaming, or non-JSON content.
    /// </summary>
    public static HttpClient Client => _client.Value;

    /// <summary>
    /// GETs <paramref name="url"/> and deserializes the JSON response body as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Response type. Must have a generated <see cref="JsonTypeInfo{T}"/> in <paramref name="ctx"/>.</typeparam>
    /// <param name="url">Absolute URL.</param>
    /// <param name="ctx">Source-generated <see cref="JsonSerializerContext"/> covering <typeparamref name="T"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deserialized response.</returns>
    /// <exception cref="HttpRequestException">Non-success HTTP status.</exception>
    /// <example>
    /// <code>
    /// record Repo(string full_name, int stargazers_count);
    ///
    /// [JsonSerializable(typeof(Repo))]
    /// partial class ApiCtx : JsonSerializerContext { }
    ///
    /// var r = await Http.GetJson&lt;Repo&gt;("https://api.github.com/repos/dotnet/runtime", ApiCtx.Default);
    /// WriteLine($"{r.full_name}: {r.stargazers_count} stars");
    /// </code>
    /// </example>
    public static async Task<T> GetJson<T>(string url, JsonSerializerContext ctx, CancellationToken ct = default)
    {
        var typeInfo = GetTypeInfo<T>(ctx);
        using var resp = await Client.GetAsync(url, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var result = await JsonSerializer.DeserializeAsync(stream, typeInfo, ct).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException($"Deserialization returned null for {typeof(T)}");
    }

    /// <summary>
    /// POSTs <paramref name="body"/> as JSON to <paramref name="url"/> and
    /// deserializes the JSON response as <typeparamref name="TResp"/>.
    /// </summary>
    /// <typeparam name="TReq">Request body type.</typeparam>
    /// <typeparam name="TResp">Response body type.</typeparam>
    /// <param name="url">Absolute URL.</param>
    /// <param name="body">Object to serialize as the request body.</param>
    /// <param name="ctx">Source-generated context covering both <typeparamref name="TReq"/> and <typeparamref name="TResp"/>.</param>
    /// <param name="ct">Cancellation token.</param>
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

    /// <summary>GETs <paramref name="url"/> and returns the response body as a string.</summary>
    public static Task<string> GetText(string url, CancellationToken ct = default) =>
        Client.GetStringAsync(url, ct);

    /// <summary>
    /// GETs <paramref name="url"/> and streams the response body to a file at <paramref name="path"/>,
    /// overwriting any existing file. Suitable for large binaries.
    /// </summary>
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
