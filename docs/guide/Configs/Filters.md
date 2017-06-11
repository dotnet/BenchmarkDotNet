# Filters

Sometimes you don't want to run all of your benchmarks.
In this case, you can *filter* some of them with the help of *filters*.

Predefined filters:

* `SimpleFilter`
* `NameFilter`
* `DisjunctionFilter`
* `CategoryFilter`
* `AnyCategoriesFilter`
* `AllCategoriesFilter`

Usage examples:

```cs
[Config(typeof(Config))]
public class IntroFilters
{
    private class Config : ManualConfig
    {
        // We will benchmark ONLY method with names (which contains "A" OR "1") AND (have length < 3)
        public Config()
        {
            Add(new DisjunctionFilter(
                new NameFilter(name => name.Contains("A")),
                new NameFilter(name => name.Contains("1"))
            )); // benchmark with names which contains "A" OR "1"
            Add(new NameFilter(name => name.Length < 3)); // benchmark with names with length < 3
        }
    }

    [Benchmark] public void A1() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void A2() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void A3() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void B1() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void B2() => Thread.Sleep(10);
    [Benchmark] public void B3() => Thread.Sleep(10);
    [Benchmark] public void C1() => Thread.Sleep(10); // Will be benchmarked
    [Benchmark] public void C2() => Thread.Sleep(10);
    [Benchmark] public void C3() => Thread.Sleep(10);
    [Benchmark] public void Aaa() => Thread.Sleep(10);
}
```

```cs
[DryJob]
[CategoriesColumn]
[BenchmarkCategory("Awesome")]
[AnyCategoriesFilter("A", "1")]
public class IntroCategories
{
    [Benchmark]
    [BenchmarkCategory("A", "1")]
    public void A1() => Thread.Sleep(10); // Will be benchmarked
    
    [Benchmark]
    [BenchmarkCategory("A", "2")]
    public void A2() => Thread.Sleep(10); // Will be benchmarked

    [Benchmark]
    [BenchmarkCategory("B", "1")]
    public void B1() => Thread.Sleep(10); // Will be benchmarked
    
    [Benchmark]
    [BenchmarkCategory("B", "2")]
    public void B2() => Thread.Sleep(10);
}
```

Command line examples:

```
--category=A
--allCategories=A,B
--anyCategories=A,B
```

If you are using `BenchmarkSwitcher` and want to run all the benchmarks with a category from all types and join them into one summary table, use the `--join` option (or `BenchmarkSwitcher.RunAllJoined`):

```
* --join --category=MyAwesomeCategory
```
