---
uid: BenchmarkDotNet.Samples.IntroExportXml
---

## Sample: IntroExportXml

BenchmarkDotNet has a set of XML exporters. You can customize the following properties of these exporters:

* `fileNameSuffix`: a string which be placed in the end of target file name.
* `indentXml`=`false`/`true`: should we format xml or not.
* `excludeMeasurements`=`false`/`true`: should we exclude detailed information about measurements or not
  (the final summary with statistics will be in the XML file anyway).


### Source code

[!code-csharp[IntroExportXml.cs](../../../samples/BenchmarkDotNet.Samples/IntroExportXml.cs)]

### Output

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

### Links

* @docs.exporters
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroExportXml

---