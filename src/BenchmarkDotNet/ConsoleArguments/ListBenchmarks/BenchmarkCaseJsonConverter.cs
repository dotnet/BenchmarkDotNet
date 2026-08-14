using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Running;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BenchmarkDotNet.ConsoleArguments.ListBenchmarks;

internal class BenchmarkCaseJsonConverter : JsonConverter<BenchmarkCase>
{
    public override void Write(Utf8JsonWriter writer, BenchmarkCase value, JsonSerializerOptions options)
    {
        var benchmarkCase = value;

        var descriptor = benchmarkCase.Descriptor;
        var type = descriptor.Type;
        var workloadMethod = descriptor.WorkloadMethod;

        writer.WriteStartObject();
        writer.WriteProperty("uid", UniqueIdGenerator.FromBenchmarkCase(benchmarkCase)); // TODO: Replace to custom uid implementation
        writer.WriteProperty("filterName", descriptor.GetFilterName());
        writer.WriteProperty("fullName", FullNameProvider.GetBenchmarkName(benchmarkCase));
        writer.WriteArrayProperty("categories", descriptor.Categories);

        // Write `job` object
        var job = benchmarkCase.Job;
        writer.WriteStartObject("job");
        writer.WriteString("id", job.ResolvedId);
        var characteristics = job.GetCharacteristicsWithValues().Where(c => c.IsPresentableCharacteristic()).ToArray();
        if (characteristics.Length > 0)
        {
            writer.WriteStartArray("characteristics");
            foreach (var characteristic in characteristics)
            {
                writer.WriteStartObject();
                writer.WriteString("key", characteristic.Id);
                writer.WriteString("value", CharacteristicPresenter.DefaultPresenter.ToPresentation(job, characteristic));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();

        // Write `location` object
        var benchmarkAttribute = workloadMethod.ResolveAttribute<BenchmarkAttribute>()!;
        writer.WriteStartObject("location");
        writer.WriteString("sourceCodeFile", benchmarkAttribute.SourceCodeFile);
        writer.WriteNumber("sourceCodeLineNumber", benchmarkAttribute.SourceCodeLineNumber);
        writer.WriteEndObject();

        writer.WriteEndObject();
    }

    public override BenchmarkCase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();
}


file static class ExtensionMethods
{
    public static void WriteProperty(this Utf8JsonWriter writer, string propertyName, ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return;

        writer.WriteString(propertyName, value);
    }

    public static void WriteArrayProperty(this Utf8JsonWriter writer, string propertyName, ReadOnlySpan<string> values)
    {
        if (values.IsEmpty)
            return;

        writer.WriteStartArray(propertyName);
        foreach (var value in values)
            writer.WriteStringValue(value);
        writer.WriteEndArray();
    }
}
