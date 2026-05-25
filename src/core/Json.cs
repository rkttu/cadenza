using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Cadenza;

/// <summary>
/// JSON helpers that require an explicit <see cref="JsonSerializerContext"/>
/// so scripts stay AOT-clean by construction. There are intentionally no
/// reflection-based overloads.
/// </summary>
public static class Json
{
    /// <summary>
    /// Deserializes <paramref name="json"/> into <typeparamref name="T"/> using
    /// the type info from <paramref name="ctx"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// record Config(string Endpoint, int Timeout);
    ///
    /// [JsonSerializable(typeof(Config))]
    /// partial class Ctx : JsonSerializerContext { }
    ///
    /// var cfg = Json.Parse&lt;Config&gt;(ReadText("config.json"), Ctx.Default);
    /// </code>
    /// </example>
    public static T Parse<T>(string json, JsonSerializerContext ctx)
    {
        var typeInfo = GetTypeInfo<T>(ctx);
        return JsonSerializer.Deserialize(json, typeInfo)
            ?? throw new InvalidOperationException($"Deserialization returned null for {typeof(T)}");
    }

    /// <summary>
    /// Serializes <paramref name="value"/> to a JSON string using the type info
    /// from <paramref name="ctx"/>.
    /// </summary>
    public static string Stringify<T>(T value, JsonSerializerContext ctx)
    {
        var typeInfo = GetTypeInfo<T>(ctx);
        return JsonSerializer.Serialize(value, typeInfo);
    }

    private static JsonTypeInfo<T> GetTypeInfo<T>(JsonSerializerContext ctx) =>
        ctx.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
            ?? throw new InvalidOperationException(
                $"JsonSerializerContext {ctx.GetType().Name} does not contain JsonTypeInfo for {typeof(T)}");
}
