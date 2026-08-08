using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks;

internal class JsonBenchmarkCasesPrinter : IBenchmarkCasesPrinter
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new BenchmarkCaseJsonConverter(),
        },
        // Disable escaping non ASCII chars. https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding#serialize-all-characters
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public void Print(IEnumerable<BenchmarkCase> benchmarkCases, ILogger logger)
    {
        var json = JsonSerializer.Serialize(benchmarkCases, DefaultOptions);
        logger.WriteLine(json);
    }
}
