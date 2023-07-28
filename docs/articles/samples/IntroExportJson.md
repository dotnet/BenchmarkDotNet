---
uid: BenchmarkDotNet.Samples.IntroExportJson
---

## Sample: IntroExportJson

BenchmarkDotNet has a set of json exporters. You can customize the following properties of these exporters:

* `fileNameSuffix`: a string which be placed in the end of target file name.
* `indentJson`=`false`/`true`: should we format json or not.
* `excludeMeasurements`=`false`/`true`: should we exclude detailed information about measurements or not
  (the final summary with statistics will be in the json file anyway).

### Source code

[!code-csharp[IntroExportJson.cs](../../../samples/BenchmarkDotNet.Samples/IntroExportJson.cs)]

### Output

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
            "IterationCount":"Auto",
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
            "IterationCount":"Auto",
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

### Links

* @docs.exporters
* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroExportJson

---
