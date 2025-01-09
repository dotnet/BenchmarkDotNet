using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Exporters
{
    public enum IntegratedExportEnum
    {
        HtmlExporterWithRPlotExporter,
    }

    public class IntegratedExport
    {
        public IntegratedExportEnum ExportEnum { get; set; }
        public IExporter Exporter { get; set; }
        public IExporter WithExporter { get; set; }
        public List<IExporter> Dependencies { get; set; }
    }

    public static class IntegratedExportersMap
    {
        private static readonly Dictionary<IntegratedExportEnum, List<string>> ExportTypesDictionary = new Dictionary<IntegratedExportEnum, List<string>>
        {
            { IntegratedExportEnum.HtmlExporterWithRPlotExporter, new List<string> { nameof(RPlotExporter), nameof(HtmlExporter) } }
        };

        public static IReadOnlyDictionary<IntegratedExportEnum, List<string>> ExportTypes => ExportTypesDictionary;

        public static string[] SplitEnumByWith(IntegratedExportEnum enumValue)
        {
            string enumString = enumValue.ToString();

            return enumString.Split(new string[] { "With" }, StringSplitOptions.None);
        }
    }
}
