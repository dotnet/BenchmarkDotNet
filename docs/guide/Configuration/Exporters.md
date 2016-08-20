# Exporters

An *exporter* allows you to export results of your benchmark in different formats. By default, files with results will be located in 
`.\BenchmarkDotNet.Artifacts\results` directory. Default exporters are: csv, html and markdown. 
Here is list of all available exporters:

## MiscExporters

There are some predefined exporters. Examples:

```cs
[AsciiDocExporter]
[CsvExporter]
[CsvMeasurementsExporter]
[HtmlExporter]
[MarkdownExporter]
[PlainExporter]
public class MyBenchmarkClass
```

## Json

BenchmarkDotNet has a set of json exporters. You can customize the following properties of these exporters:

* `fileNameSuffix`: a string which be placed in the end of target file name.
* `indentJson`=`false`/`true`: should we format json or not.
* `excludeMeasurements`=`false`/`true`: should we excldue detailed information about measurements or not (the final summary with statistics will be in the json file anyway),

Also there are a set of predefined json exporters. Example of usage:

```cs
[JsonExporterAttribute.Brief]
[JsonExporterAttribute.Full]
[JsonExporterAttribute.BriefCompressed]
[JsonExporterAttribute.FullCompressed]
[JsonExporter("-custom", indentJson: true, excludeMeasurements: true)]
public class IntroJsonExport
{
    [Benchmark] public void Sleep10() => Thread.Sleep(10);
    [Benchmark] public void Sleep20() => Thread.Sleep(20);
}
```

```cs
[Config(typeof(Config))]
public class IntroJsonExport2
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(JsonExporter.Brief);
            Add(JsonExporter.Full);
            Add(JsonExporter.BriefCompressed);
            Add(JsonExporter.FullCompressed);
            Add(JsonExporter.Custom("-custom", indentJson: true, excludeMeasurements: true));
        }
    }
}
```

Example of `IntroJsonExport-report-brief.json`:

```js
{
   "Title":"IntroJsonExport",
   "HostEnvironmentInfo":{
      "BenchmarkDotNetCaption":"BenchmarkDotNet-Dev.Core",
      "BenchmarkDotNetVersion":"0.9.9.0",
      "OsVersion":"Microsoft Windows NT 6.2.9200.0",
      "ProcessorName":{
         "IsValueCreated":true,
         "Value":"Intel(R) Core(TM) i7-4702MQ CPU 2.20GHz"
      },
      "ProcessorCount":8,
      "ClrVersion":"MS.NET 4.0.30319.42000",
      "Architecture":"64-bit",
      "HasAttachedDebugger":false,
      "HasRyuJit":true,
      "Configuration":"RELEASE",
      "JitModules":"clrjit-v4.6.1586.0",
      "DotNetCliVersion":"1.0.0-preview2-003121",
      "ChronometerFrequency":2143474,
      "HardwareTimerKind":"Tsc"
   },
   "Benchmarks":[
      {
         "ShortInfo":"IntroJsonExport_Sleep10",
         "Type":"IntroJsonExport",
         "Method":"Sleep10",
         "MethodTitle":"Sleep10",
         "Parameters":"",
         "Properties":{
            "Mode":"Throughput",
            "Platform":"Host",
            "Jit":"Host",
            "Runtime":"Host",
            "GcMode":"Host",
            "WarmupCount":"Auto",
            "TargetCount":"Auto",
            "LaunchCount":"Auto",
            "IterationTime":"Auto",
            "Affinity":"Auto"
         },
         "Statistics":{
            "N":20,
            "Min":10287928.031875,
            "LowerFence":10229901.2459375,
            "Q1":10352130.9040625,
            "Median":10393069.119375,
            "Mean":10390116.844234375,
            "Q3":10433617.3428125,
            "UpperFence":10555847.000937503,
            "Max":10473396.5165625,
            "InterquartileRange":81486.438750000671,
            "Outliers":[
               
            ],
            "StandardError":12514.907849226243,
            "Variance":3132458369.4924932,
            "StandardDeviation":55968.36936603114,
            "Skewness":-0.26688070240057665,
            "Kurtosis":1.87502014055259,
            "ConfidenceInterval":{
               "Mean":10390116.844234375,
               "Error":12514.907849226243,
               "Level":6,
               "Margin":24529.219384483436,
               "Lower":10365587.624849891,
               "Upper":10414646.063618859
            },
            "Percentiles":{
               "P0":10287928.031875,
               "P25":10354961.078906249,
               "P50":10393069.119375,
               "P67":10423662.05225,
               "P80":10439024.7445,
               "P85":10447944.260171875,
               "P90":10461577.210718751,
               "P95":10465571.164984375,
               "P100":10473396.5165625
            }
         }
      },{
         "ShortInfo":"IntroJsonExport_Sleep20",
         "Type":"IntroJsonExport",
         "Method":"Sleep20",
         "MethodTitle":"Sleep20",
         "Parameters":"",
         "Properties":{
            "Mode":"Throughput",
            "Platform":"Host",
            "Jit":"Host",
            "Runtime":"Host",
            "GcMode":"Host",
            "WarmupCount":"Auto",
            "TargetCount":"Auto",
            "LaunchCount":"Auto",
            "IterationTime":"Auto",
            "Affinity":"Auto"
         },
         "Statistics":{
            "N":20,
            "Min":20263046.11125,
            "LowerFence":20185645.475625,
            "Q1":20313096.286875002,
            "Median":20336976.9121875,
            "Mean":20351602.702031251,
            "Q3":20398063.494375,
            "UpperFence":20525514.305625,
            "Max":20464617.251875,
            "InterquartileRange":84967.207499999553,
            "Outliers":[
               
            ],
            "StandardError":11991.004261727438,
            "Variance":2875683664.0953112,
            "StandardDeviation":53625.401295424461,
            "Skewness":0.15925629564588534,
            "Kurtosis":2.0971344882863447,
            "ConfidenceInterval":{
               "Mean":20351602.702031251,
               "Error":11991.004261727438,
               "Level":6,
               "Margin":23502.368352985777,
               "Lower":20328100.333678264,
               "Upper":20375105.070384238
            },
            "Percentiles":{
               "P0":20263046.11125,
               "P25":20313438.8965625,
               "P50":20336976.9121875,
               "P67":20388224.909718752,
               "P80":20400439.8935,
               "P85":20405415.752406247,
               "P90":20409280.681625,
               "P95":20413094.583750002,
               "P100":20464617.251875
            }
         }
      }
   ]
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
