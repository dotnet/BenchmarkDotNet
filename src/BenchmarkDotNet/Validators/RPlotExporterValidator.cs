using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;

namespace BenchmarkDotNet.Validators
{
    public class RPlotExporterValidator : IValidator
    {
        public static readonly RPlotExporterValidator FailOnError = new RPlotExporterValidator();

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var exporters = validationParameters.Config.GetExporters();
            if (!ContainRPlotExporter(exporters))
                yield break;

            if (!ValidateCsvExporter(exporters))
                yield return new ValidationError(TreatsWarningsAsErrors, "RPlotExporter requires CsvMeasurementsExporter.Default. Do not override CsvMeasurementsExporter");
        }

        private static bool ContainRPlotExporter(IEnumerable<IExporter> exporters)
        {
            return exporters.Any(exporter => exporter.GetType() == typeof(RPlotExporter));
        }

        private static bool ValidateCsvExporter(IEnumerable<IExporter> exporters)
        {
            var exporter = exporters.Cast<CsvMeasurementsExporter>().FirstOrDefault();
            if (exporter == null)
                return false;

            return exporter.Separator == CsvMeasurementsExporter.Default.Separator;
        }
    }
}