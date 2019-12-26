Imports BenchmarkDotNet.Analysers
Imports BenchmarkDotNet.Attributes
Imports BenchmarkDotNet.Columns
Imports BenchmarkDotNet.Configs
Imports BenchmarkDotNet.Exporters
Imports BenchmarkDotNet.Exporters.Csv
Imports BenchmarkDotNet.Jobs
Imports BenchmarkDotNet.Loggers

Namespace _BenchmarkProjectName_
    Public Class BenchmarkConfig
        Inherits ManualConfig

        Public Sub New()
            ' Configure your benchmarks, see for more details: https://benchmarkdotnet.org/articles/configs/configs.html.
            ' Add(Job.Dry);
            ' Add(ConsoleLogger.Default);
            ' Add(TargetMethodColumn.Method, StatisticColumn.Max);
            ' Add(RPlotExporter.Default, CsvExporter.Default);
            ' Add(EnvironmentAnalyser.Default);
        End Sub
    End Class
End Namespace
