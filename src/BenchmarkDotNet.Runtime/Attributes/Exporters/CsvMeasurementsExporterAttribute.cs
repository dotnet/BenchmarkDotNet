﻿using BenchmarkDotNet.Exporters.Csv;

namespace BenchmarkDotNet.Attributes.Exporters
{
    public class CsvMeasurementsExporterAttribute : ExporterConfigBaseAttribute
    {
        public CsvMeasurementsExporterAttribute(CsvSeparator separator = CsvSeparator.CurrentCulture) : base(new CsvMeasurementsExporter(separator))
        {
        }
    }
}