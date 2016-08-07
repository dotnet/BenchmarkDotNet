# Exporters

An *exporter* allows you to export results of your benchmark in different formats. By default, files with results will be located in 
`.\BenchmarkDotNet.Artifacts\results` directory. Default exporters are: csv, html and markdown. 
Here is list of all available exporters:

```cs
public IEnumerable<IExporter> GetExporters()
{
    yield return MarkdownExporter.Default; // produces <BenchmarkName>-report-default.md
    yield return MarkdownExporter.GitHub; // produces <BenchmarkName>-report-github.md
    yield return MarkdownExporter.StackOverflow; // produces <BenchmarkName>-report-stackoverflow.md
    yield return CsvExporter.Default; // produces <BenchmarkName>-report.csv
    yield return CsvMeasurementsExporter.Default; // produces <BenchmarkName>-measurements.csv
    yield return HtmlExporter.Default; // produces <BenchmarkName>-report.html
    yield return PlainExporter.Default; // produces <BenchmarkName>-report.txt
}
```

## Plots

If you have installed [R](https://www.r-project.org/), defined `%R_HOME%` variable and used `RPlotExporter.Default` and `CsvMeasurementsExporter.Default` 
in your config, you will also get nice plots with help of the `BuildPlots.R` script in your bin directory. 

### Examples

```
<BenchmarkName>-barplot.png
<BenchmarkName>-boxplot.png
<BenchmarkName>-<MethodName>-density.png
<BenchmarkName>-<MethodName>-facetTimeline.png
<BenchmarkName>-<MethodName>-facetTimelineSmooth.png
<BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png
<BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png
```

A config example:

```cs
public class Config : ManualConfig
{
    public Config()
    {
        Add(CsvMeasurementsExporter.Default);
        Add(RPlotExporter.Default);
    }
}
```
