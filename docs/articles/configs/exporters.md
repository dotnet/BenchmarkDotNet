---
uid: docs.exporters
name: Exporters
---

# Exporters

An *exporter* allows you to export results of your benchmark in different formats.
By default, files with results will be located in 
`.\BenchmarkDotNet.Artifacts\results` directory, but this can be changed via the `ArtifactsPath` property in the `IConfig`. 
Default exporters are: csv, html and markdown.

---

[!include[IntroExport](../samples/IntroExport.md)]

[!include[IntroExportJson](../samples/IntroExportJson.md)]

[!include[IntroExportXml](../samples/IntroExportXml.md)]


## Plots

[You can install R](https://www.r-project.org/) to automatically get nice plots of your benchmark results.
First, make sure `Rscript.exe` or `Rscript` is in your path,
  or define an R_HOME environment variable pointing to the R installation directory.  
_eg: If `Rscript` is located in `/path/to/R/R-1.2.3/bin/Rscript`, then `R_HOME` must point to `/path/to/R/R-1.2.3/`, it **should not** point to `/path/to/R/R-1.2.3/bin`_  
Use `RPlotExporter.Default` and `CsvMeasurementsExporter.Default` in your config,
  and the `BuildPlots.R` script in your bin directory will take care of the rest.

Examples:

```
<BenchmarkName>-barplot.png
<BenchmarkName>-boxplot.png
<BenchmarkName>-<MethodName>-density.png
<BenchmarkName>-<MethodName>-facetTimeline.png
<BenchmarkName>-<MethodName>-facetTimelineSmooth.png
<BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png
<BenchmarkName>-<MethodName>-<JobName>-timelineSmooth.png
```

A config example in C#:

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

A config example in F#:

```fs
module MyBenchmark

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Exporters
open BenchmarkDotNet.Exporters.Csv
open MyProjectUnderTest

type MyConfig() as this =
    inherit ManualConfig()
    do
        this.Add(CsvMeasurementsExporter.Default)
        this.Add(RPlotExporter.Default)

[<
  MemoryDiagnoser; 
  Config(typeof<MyConfig>);
  RPlotExporter
>]
type MyPerformanceTests() =

    let someTestData = getTestDataAsList ()

    [<Benchmark>]
    member __.SomeTestCase() = 
        someTestData |> myFunctionUnderTest
```

## CSV

The CSV file format is often used to graph the output or to analyze the results programmatically. The CSV exporter may be configured to produce sanitized output, where cell values are numerals and their units are predefined.

The CSV exporter and other compatible exporters may consume an instance of `ISummaryStyle` that defines how the output should look like:

| Property            | Remarks                                            | Default |
| ------------------- | -------------------------------------------------- | ------- |
| PrintUnitsInHeader  | If true, units will be displayed in the header row | false   |
| PrintUnitsInContent | If true, units will be appended to the value       | true    |
| TimeUnit            | If null, unit will be automatically selected       | null    |
| SizeUnit            | If null, unit will be automatically selected       | null    |

Example of CSV exporter configured to always use microseconds, kilobytes, and to render units only in column headers:

```cs
var exporter = new CsvExporter(
    CsvSeparator.CurrentCulture,
    new SummaryStyle(
        cultureInfo: System.Globalization.CultureInfo.CurrentCulture,
        printUnitsInHeader: true,
        printUnitsInContent: false,
        timeUnit: Perfolizer.Horology.TimeUnit.Microsecond,
        sizeUnit: SizeUnit.KB
    ));

var config = ManualConfig.CreateMinimumViable().AddExporter(exporter);
```

Excerpt from the resulting CSV file:

```
Method,...,Mean [us],Error [us],StdDev [us],Min [us],Max [us],Allocated [KB]
Benchmark,...,"37,647.6","32,717.9","21,640.9","11,209.2","59,492.6",1.58
```
