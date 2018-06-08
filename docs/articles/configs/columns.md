# Columns

A *column* is a column in the summary table.

## Default columns

In this section, default columns (which be added to the Summary table by default) are presented. Some of columns are optional, i.e. they can be omitted (it depends on the measurements from the summary).

### Target

There are 3 default columns which describes the target benchmark: `Namespace`, `Type`, `Method`. `Namespace` and `Type` will be omitted when all the benchmarks have the same namespace or type name. `Method` column always be a part of the summary table.

### Job

There are many different job characteristics, but the summary includes only characteristics which has at least one non-default value.

### Statistics
There are also a lot of different statistics which can be considered. It will be really hard to analyse the summary table, if all of the available statistics will be shown. Fortunately, BenchmarkDotNet has some heuristics for statistics columns and shows only important columns. For example, if all of the standard deviations are zero (we run our benchmarks against Dry job), this column will be omitted. The standard error will be shown only for cases when we are failed to achieve required accuracy level.

Only `Mean` will be always shown. If the distribution looks strange, BenchmarkDotNet could also print additional columns like `Median` or `P95` (95th percentile).

If you need specific statistics, you always could add them manually.

### Params
If you have `params`, the corresponded columns will be automatically added.

### Diagnosers
If you turned on diagnosers which providers additional columns, they will be also included in the summary page.

## Optional columns

### TagColumns

In the following example, we introduce two new columns which contains a tag based on a benchmark method name:

```cs
[Config(typeof(Config))]
public class IntroTags
{
    private class Config : ManualConfig
    {
        public Config()
        {
            // You can add custom tags per each method using Columns
            Add(new TagColumn("Kind", name => name.Substring(0, 3)));
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

| Method | Mean       | Kind | Number |
| ------ | ---------- | ---- | ------ |
| Bar34  | 10.3636 ms | Bar  | 34     |
| Bar3   | 10.4662 ms | Bar  | 3      |
| Foo12  | 10.1377 ms | Foo  | 12     |
| Foo1   | 10.2814 ms | Foo  | 1      |


### RankColumn
`RankColumn` allows you to rank your benchmarks. If there is no significant difference between two benchmarks, they will have the same rank. It also makes sense to define a `SummaryOrderPolicy` (e.g. `FastestToSlowest` or `SlowestToFastest`) when you are using `RankColumn`. An example:

```cs
[OrderProvider(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[RankColumn(NumeralSystem.Roman)]
public class IntroRankColumn
{
    [Params(1, 2)]
    public int Factor;

    [Benchmark]
    public void Foo() => Thread.Sleep(Factor * 100);

    [Benchmark]
    public void Bar() => Thread.Sleep(Factor * 200);
}
```

Result:

```
 Method | Factor |        Mean | Rank | Rank |
------- |------- |------------ |----- |----- |
    Foo |      1 | 100.2541 ms |    1 |    I |
    Foo |      2 | 200.3021 ms |    2 |   II |
    Bar |      1 | 200.3863 ms |    2 |   II |
    Bar |      2 | 400.3847 ms |    3 |  III |
```


## Custom columns

Of course, you can define own custom columns and use it everywhere. Here is the definition of `TagColumn`:

```cs
public class TagColumn : IColumn
{
    private readonly Func<string, string> getTag;

    public string ColumnName { get; }

    public TagColumn(string columnName, Func<string, string> getTag)
    {
        this.getTag = getTag;
        ColumnName = columnName;
    }

    public bool IsDefault(Summary summary, Benchmark benchmark) => false;
    public string GetValue(Summary summary, Benchmark benchmark) => getTag(benchmark.Target.Method.Name);

    public bool IsAvailable(Summary summary) => true;
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public override string ToString() => ColumnName;
}
```