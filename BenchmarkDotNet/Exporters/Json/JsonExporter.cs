namespace BenchmarkDotNet.Exporters.Json
{
    public class JsonExporter : JsonExporterBase, IExporter
    {
        public static readonly IExporter Default = new JsonExporter();

        public JsonExporter() : base(indentJson: false, excludeMeasurments: false)
        {
        }
    }
}
