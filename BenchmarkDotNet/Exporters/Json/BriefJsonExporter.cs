namespace BenchmarkDotNet.Exporters.Json
{
    public class BriefJsonExporter : JsonExporterBase
    {
        public static readonly IExporter Default = new BriefJsonExporter();

        protected override string FileNameSuffix => "-brief";

        public BriefJsonExporter() : base(indentJson: true, excludeMeasurements: true)
        {
        }
    }
}
