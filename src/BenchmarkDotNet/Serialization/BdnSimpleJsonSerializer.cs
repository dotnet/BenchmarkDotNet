using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace BenchmarkDotNet.Serialization;

/// <summary>
/// JsonSerializer that compatible with JsonExporter format.
/// </summary>
internal static class BdnSimpleJsonSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        Converters =
        {
            new SimpleJsonFloatConverter(),
            new SimpleJsonDoubleConverter(),
        },
        // Disable escaping non ASCII chars. https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding#serialize-all-characters
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolverChain =
        {
            // Use Reflection based serialization/deserialization. Because Summary object contains anonymous type.
            new DefaultJsonTypeInfoResolver(),
        },
        RespectNullableAnnotations = true,
    };

    private static readonly JsonSerializerOptions IndentedOptions = new(DefaultOptions)
    {
        WriteIndented = true,
    };

    public static string Serialize<T>(T item, bool indentJson = false)
    {
        if (indentJson)
            return JsonSerializer.Serialize(item, IndentedOptions);
        else
            return JsonSerializer.Serialize(item, DefaultOptions);
    }

    /// <summary>
    /// Custom JsonConverter for float that write Nan/PositiveInfinite/NegativeInfinite as empty string.
    /// </summary>
    private class SimpleJsonFloatConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetSingle();

            throw new JsonException($"Unexpected token {reader.TokenType} when parsing float.");
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                writer.WriteStringValue("");
            else
                writer.WriteNumberValue(value);
        }
    }

    /// <summary>
    /// Custom JsonConverter for double that write Nan/PositiveInfinite/NegativeInfinite as empty string.
    /// </summary>
    private class SimpleJsonDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetSingle();

            throw new JsonException($"Unexpected token {reader.TokenType} when parsing double.");
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                writer.WriteStringValue("");
            else
                writer.WriteNumberValue(value);
        }
    }
}
