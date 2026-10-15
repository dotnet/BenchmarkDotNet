using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using System.Text.Json.Serialization;

namespace BenchmarkDotNet.Serialization;

/// <summary>
/// Metadata for what <see cref="JsonExporterBase"/> exports. Its values are typed <see cref="object"/>, so every
/// runtime type a value can have is listed here, which lets the exporter run without reflection (Native AOT).
/// </summary>
[JsonSerializable(typeof(IReadOnlyDictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, object?>[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(Frequency))]
[JsonSerializable(typeof(Statistics))]
[JsonSerializable(typeof(JsonMemoryData))]
[JsonSerializable(typeof(JsonMeasurementData[]))]
[JsonSerializable(typeof(Metric[]))]
internal partial class BdnSimpleJsonSerializerContext : JsonSerializerContext
{
}
