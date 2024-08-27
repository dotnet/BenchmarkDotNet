using System;
using BenchmarkDotNet.Exporters;

namespace BenchmarkDotNet.Attributes;

/// <summary>
/// IMPORTANT: Not fully implemented yet
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class PhdExporterAttribute() : ExporterConfigBaseAttribute(new PhdJsonExporter(), new PhdMdExporter());