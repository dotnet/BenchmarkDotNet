namespace BenchmarkDotNet.Exporters.Json
{
    public class FormattedJsonExporter : JsonExporterBase
    {
        public static readonly IExporter Default = new FormattedJsonExporter();

        protected override string FileNameSuffix => "-formatted";

        public FormattedJsonExporter() : base(indentJson: true, excludeMeasurements: false)
        {
        }
    }
}
