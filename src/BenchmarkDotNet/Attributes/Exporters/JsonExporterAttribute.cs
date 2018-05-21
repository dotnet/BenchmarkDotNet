﻿using System;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;

namespace BenchmarkDotNet.Attributes.Exporters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class JsonExporterAttribute : ExporterConfigBaseAttribute
    {
        protected JsonExporterAttribute(IExporter exporter) : base(exporter)
        {
        }

        public JsonExporterAttribute(string fileNameSuffix = "", bool indentJson = false, bool excludeMeasurements = false)
            : this(new JsonExporter(fileNameSuffix, indentJson, excludeMeasurements))
        {
        }

        public class BriefAttribute : JsonExporterAttribute
        {
            public BriefAttribute() : base(JsonExporter.Brief)
            {
            }
        }

        public class Full : JsonExporterAttribute
        {
            public Full() : base(JsonExporter.Full)
            {
            }
        }

        public class BriefCompressed : JsonExporterAttribute
        {
            public BriefCompressed() : base(JsonExporter.BriefCompressed)
            {
            }
        }

        public class FullCompressed : JsonExporterAttribute
        {
            public FullCompressed() : base(JsonExporter.FullCompressed)
            {
            }
        }
    }
}