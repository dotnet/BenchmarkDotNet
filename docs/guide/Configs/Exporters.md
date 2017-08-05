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
* `excludeMeasurements`=`false`/`true`: should we exclude detailed information about measurements or not (the final summary with statistics will be in the json file anyway).

Also, there are a set of predefined json exporters. Example of usage:

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
         "Namespace":"BenchmarkDotNet.Samples.Intro",
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
            "Min":10265993.7209375,
            "LowerFence":10255329.082734371,
            "Q1":10337369.528437499,
            "Median":10360382.6953125,
            "Mean":10362283.085796878,
            "Q3":10392063.158906251,
            "UpperFence":10474103.60460938,
            "Max":10436008.3209375,
            "InterquartileRange":54693.630468752235,
            "Outliers":[
               
            ],
            "StandardError":10219.304338928456,
            "Variance":2088683623.4328396,
            "StandardDeviation":45702.118369205156,
            "Skewness":-0.1242777170069375,
            "Kurtosis":2.31980277935226,
            "ConfidenceInterval":{
               "Mean":10362283.085796878,
               "Error":10219.304338928456,
               "Level":6,
               "Margin":20029.836504299772,
               "Lower":10342253.249292579,
               "Upper":10382312.922301177
            },
            "Percentiles":{
               "P0":10265993.7209375,
               "P25":10338555.905625,
               "P50":10360382.6953125,
               "P67":10373496.555659376,
               "P80":10400703.4841875,
               "P85":10417280.326718749,
               "P90":10424125.595812501,
               "P95":10435620.51609375,
               "P100":10436008.3209375
            }
         }
      },{
         "ShortInfo":"IntroJsonExport_Sleep20",
         "Namespace":"BenchmarkDotNet.Samples.Intro",
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
            "Min":20258672.37,
            "LowerFence":20206333.269843742,
            "Q1":20325342.761249997,
            "Median":20362636.192500003,
            "Mean":20360791.931687497,
            "Q3":20404682.4221875,
            "UpperFence":20523691.913593754,
            "Max":20422396.073125,
            "InterquartileRange":79339.66093750298,
            "Outliers":[
               
            ],
            "StandardError":10728.817907277158,
            "Variance":2302150673.7502208,
            "StandardDeviation":47980.732317777525,
            "Skewness":-0.50826238372439869,
            "Kurtosis":2.11050327966268,
            "ConfidenceInterval":{
               "Mean":20360791.931687497,
               "Error":10728.817907277158,
               "Level":6,
               "Margin":21028.48309826323,
               "Lower":20339763.448589232,
               "Upper":20381820.414785761
            },
            "Percentiles":{
               "P0":20258672.37,
               "P25":20327638.975312497,
               "P50":20362636.192500003,
               "P67":20391669.3762875,
               "P80":20406370.68625,
               "P85":20412542.034406248,
               "P90":20414412.5376875,
               "P95":20416606.697718751,
               "P100":20422396.073125
            }
         }
      }
   ]
}
```

## Plots

You can install [R](https://www.r-project.org/) to automatically get nice plots of your benchmark results. First, make sure `Rscript.exe` or `Rscript` is in your path, or define an R_HOME environment variable pointing to the R installation directory (containing the `bin` directory). Use `RPlotExporter.Default` and `CsvMeasurementsExporter.Default` in your config, and the `BuildPlots.R` script in your bin directory will take care of the rest.

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
var config = ManualConfig.Create(DefaultConfig.Instance);
config.Add(new CsvExporter(
    CsvSeparator.CurrentCulture,
    new BenchmarkDotNet.Reports.SummaryStyle
    {
        PrintUnitsInHeader = true,
        PrintUnitsInContent = false,
        TimeUnit = BenchmarkDotNet.Horology.TimeUnit.Microsecond,
        SizeUnit = BenchmarkDotNet.Columns.SizeUnit.KB
    }));
```

Excerpt from the resulting CSV file:

```
Method,...,Mean [us],Error [us],StdDev [us],Min [us],Max [us],Allocated [KB]
Benchmark,...,"37,647.6","32,717.9","21,640.9","11,209.2","59,492.6",1.58
```

## XML

BenchmarkDotNet has a set of XML exporters. You can customize the following properties of these exporters:

* `fileNameSuffix`: a string which be placed in the end of target file name.
* `indentXml`=`false`/`true`: should we format xml or not.
* `excludeMeasurements`=`false`/`true`: should we exclude detailed information about measurements or not (the final summary with statistics will be in the XML file anyway).

Also, there are a set of predefined XML exporters. Example of usage:

```cs
[XmlExporterAttribute.Brief]
[XmlExporterAttribute.Full]
[XmlExporterAttribute.BriefCompressed]
[XmlExporterAttribute.FullCompressed]
[XmlExporter("-custom", indentXml: true, excludeMeasurements: true)]
public class IntroXmlExport
{
    [Benchmark] public void Sleep10() => Thread.Sleep(10);
    [Benchmark] public void Sleep20() => Thread.Sleep(20);
}
```

Example of `IntroXmlExport-report-brief.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Summary>
  <Title>IntroXmlExport</Title>
  <HostEnvironmentInfo>
    <BenchmarkDotNetCaption>BenchmarkDotNet</BenchmarkDotNetCaption>
    <BenchmarkDotNetVersion>0.10.9.20170805-develop</BenchmarkDotNetVersion>
    <OsVersion>Windows 10 Redstone 2 (10.0.15063)</OsVersion>
    <ProcessorName>Intel Core i7-3770K CPU 3.50GHz (Ivy Bridge)</ProcessorName>
    <ProcessorCount>8</ProcessorCount>
    <RuntimeVersion>.NET Framework 4.7 (CLR 4.0.30319.42000)</RuntimeVersion>
    <Architecture>64bit</Architecture>
    <HasAttachedDebugger>False</HasAttachedDebugger>
    <HasRyuJit>True</HasRyuJit>
    <Configuration>RELEASE</Configuration>
    <JitModules>clrjit-v4.7.2101.1</JitModules>
    <DotNetSdkVersion>1.0.4</DotNetSdkVersion>
    <ChronometerFrequency>
      <Hertz>3410220</Hertz>
    </ChronometerFrequency>
    <HardwareTimerKind>Tsc</HardwareTimerKind>
  </HostEnvironmentInfo>
  <Benchmarks>
    <Benchmark>
      <DisplayInfo>IntroXmlExport.Sleep10: DefaultJob</DisplayInfo>
      <Namespace>BenchmarkDotNet.Samples.Intro</Namespace>
      <Type>IntroXmlExport</Type>
      <Method>Sleep10</Method>
      <MethodTitle>Sleep10</MethodTitle>
      <Statistics>
        <N>15</N>
        <Min>10989865.8785938</Min>
        <LowerFence>10989836.0967969</LowerFence>
        <Q1>10990942.6053125</Q1>
        <Median>10991249.5870313</Median>
        <Mean>10991270.0524583</Mean>
        <Q3>10991680.2776563</Q3>
        <UpperFence>10992786.7861719</UpperFence>
        <Max>10992115.5501563</Max>
        <InterquartileRange>737.672343749553</InterquartileRange>
        <StandardError>148.484545262958</StandardError>
        <Variance>330714.902729213</Variance>
        <StandardDeviation>575.07817097262</StandardDeviation>
        <Skewness>-0.67759778074187</Skewness>
        <Kurtosis>3.14296703520386</Kurtosis>
        <ConfidenceInterval>
          <N>15</N>
          <Mean>10991270.0524583</Mean>
          <StandardError>148.484545262958</StandardError>
          <Level>L999</Level>
          <Margin>614.793505974065</Margin>
          <Lower>10990655.2589524</Lower>
          <Upper>10991884.8459643</Upper>
        </ConfidenceInterval>
        <Percentiles>
          <P0>10989865.8785938</P0>
          <P25>10991027.3689063</P25>
          <P50>10991249.5870313</P50>
          <P67>10991489.490875</P67>
          <P80>10991696.7722187</P80>
          <P85>10991754.5031875</P85>
          <P90>10991933.1939688</P90>
          <P95>10992067.441125</P95>
          <P100>10992115.5501563</P100>
        </Percentiles>
      </Statistics>
    </Benchmark>
  </Benchmarks>
</Summary>
```