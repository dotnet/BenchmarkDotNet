namespace BenchmarkDotNet.Exporters.Json
{
    public class JsonExporter : JsonExporterBase
    {
        public static readonly IExporter Brief = new JsonExporter("-brief", indentJson: true, excludeMeasurements: true);
        public static readonly IExporter Full = new JsonExporter("-full", indentJson: true, excludeMeasurements: false);
        public static readonly IExporter BriefCompressed = new JsonExporter("-brief-compressed", indentJson: false, excludeMeasurements: true);
        public static readonly IExporter FullCompressed = new JsonExporter("-full-compressed", indentJson: false, excludeMeasurements: false);

        public static readonly IExporter Default = FullCompressed;

        protected override string FileNameSuffix { get; } = string.Empty;

        public JsonExporter(string fileNameSuffix = "", bool indentJson = false, bool excludeMeasurements = false) : base(indentJson, excludeMeasurements)
        {
            FileNameSuffix = fileNameSuffix;
        }

        public static IExporter Custom(string fileNameSuffix = "", bool indentJson = false, bool excludeMeasurements = false) =>
            new JsonExporter(fileNameSuffix, indentJson, excludeMeasurements);
    }
}