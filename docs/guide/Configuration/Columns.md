# Columns

A *column* is a column in the summary table.

## Predefined columns

```cs
class StatisticColumn
{
    IColumn Mean;
    IColumn StdError;
    IColumn StdDev;
    IColumn OperationPerSecond;
    IColumn Min;
    IColumn Q1;
    IColumn Median;
    IColumn Q3;
    IColumn Max;
    
    IColumn P0;
    IColumn P25;
    IColumn P50;
    IColumn P80;
    IColumn P85;
    IColumn P90;
    IColumn P95;
    IColumn P100;

    IColumn[] AllStatistics = 
				{ 
					Mean, StdError, StdDev, 
					OperationsPerSecond, Min, Q1, Median, Q3, Max 
				};
}

// Specify a "place" of each benchmark. Place 1 means a group of the fastest benchmarks, 
// place 2 means the second group, and so on. There are several styles:
class Place
{
    IColumn ArabicNumber; // `1`, `2`, `3`, ...
    IColumn Stars; // `*`, `**`, `***`, ...
}

class PropertyColumn
{
    IColumn Namespace;
    IColumn Type;
    IColumn Method;
    IColumn Mode;
    IColumn Platform;
    IColumn Jit;
    IColumn Framework;
    IColumn Toolchain;
    IColumn Runtime;
    IColumn LaunchCount;
    IColumn WarmupCount;
    IColumn TargetCount;
    IColumn Affinity;
}
```

## Default columns

* `PropertyColumn.Type`
* `PropertyColumn.Method`
* `PropertyColumn.Mode`
* `PropertyColumn.Platform`
* `PropertyColumn.Jit`
* `PropertyColumn.Framework`
* `PropertyColumn.Toolchain`
* `PropertyColumn.Runtime`
* `PropertyColumn.ProcessCount`
* `PropertyColumn.WarmupCount`
* `PropertyColumn.TargetCount`
* `PropertyColumn.Affinity`
* `StatisticColumn.Median`
* `StatisticColumn.StdDev`
* `BaselineDeltaColumn.Default`

## Examples

```cs
// *** Command style ***
[Config("columns=Min,Max")]
[Config("columns=AllStatistics")]
```

```cs
// *** Object style ***
[Config(typeof(Config))]
public class IntroTags
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(Job.Dry);
            // You can add custom tags per each method using Columns
            Add(new TagColumn("Foo or Bar", name => name.Substring(0, 3)));
            Add(new TagColumn("Number", name => name.Substring(3)));
        }
    }

    [Benchmark] public void Foo1() { /* ... */ }
    [Benchmark] public void Foo12() { /* ... */ }
    [Benchmark] public void Bar3() { /* ... */ }
    [Benchmark] public void Bar34() { /* ... */ }
}

```
Result:

 Method |     Median |    StdDev | Foo or Bar | Number |
------- |----------- |---------- |----------- |------- |
  Bar34 | 10.3636 ms | 0.0000 ms |        Bar |     34 |
   Bar3 | 10.4662 ms | 0.0000 ms |        Bar |      3 |
  Foo12 | 10.1377 ms | 0.0000 ms |        Foo |     12 |
   Foo1 | 10.2814 ms | 0.0000 ms |        Foo |      1 |
