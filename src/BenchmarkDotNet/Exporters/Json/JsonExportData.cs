namespace BenchmarkDotNet.Exporters.Json
{
    // Named instead of anonymous types so the JSON exporter can be serialized with source-generated metadata
    // (BdnSimpleJsonSerializerContext), which works without reflection in Native AOT applications.
    // The property names and their order are the exported format: don't change them.

    internal sealed class JsonMemoryData
    {
        public int Gen0Collections { get; init; }
        public int Gen1Collections { get; init; }
        public int Gen2Collections { get; init; }
        public long TotalOperations { get; init; }
        public long? BytesAllocatedPerOperation { get; init; }
    }

    internal sealed class JsonMeasurementData
    {
        public string IterationMode { get; init; } = "";
        public string IterationStage { get; init; } = "";
        public int LaunchIndex { get; init; }
        public int IterationIndex { get; init; }
        public long Operations { get; init; }
        public double Nanoseconds { get; init; }
    }
}
