using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks;

internal class JsonBenchmarkCasesPrinter : IBenchmarkCasesPrinter
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        // Disable escaping non ASCII chars. https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding#serialize-all-characters
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly BenchmarkCaseJsonConverter Converter = new();

    // The converter writes each case itself, so it is driven directly: serializing the sequence would need reflection
    // to find out how to write it, which a Native AOT application doesn't have.
    public void Print(IEnumerable<BenchmarkCase> benchmarkCases, ILogger logger)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = DefaultOptions.WriteIndented, Encoder = DefaultOptions.Encoder }))
        {
            writer.WriteStartArray();
            foreach (var benchmarkCase in benchmarkCases)
                Converter.Write(writer, benchmarkCase, DefaultOptions);
            writer.WriteEndArray();
        }
        logger.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
    }
}
