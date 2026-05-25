using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Cadenza;

public static class Json
{
    public static T Parse<T>(string json, JsonSerializerContext ctx)
    {
        var typeInfo = GetTypeInfo<T>(ctx);
        return JsonSerializer.Deserialize(json, typeInfo)
            ?? throw new InvalidOperationException($"Deserialization returned null for {typeof(T)}");
    }

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
