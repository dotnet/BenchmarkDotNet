using System;
using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class PhdExporterAttribute() : ExporterConfigBaseAttribute(new PhdJsonExporter());