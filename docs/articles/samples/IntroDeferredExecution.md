---
uid: BenchmarkDotNet.Samples.IntroDeferredExecution
---

## Sample: IntroDeferredExecution

In LINQ, execution of a query is usually [deferred](https://learn.microsoft.com/dotnet/standard/linq/deferred-execution-example) until the moment when you actually request the data. If your benchmark just returns `IEnumerable` or `IQueryable` it's not measuring the execution of the query, just the creation.

This is why we decided to warn you about this issue whenever it happens:

```log
Benchmark IntroDeferredExecution.Wrong returns a deferred execution result (IEnumerable<Int32>). You need to either change the method declaration to return a materialized result or consume it on your own. You can use .Consume() extension method to do that.
```

Don't worry! We are also providing you with a `Consume` extension method which can execute given `IEnumerable` or `IQueryable` and consume its results. All you need to do is to create a [`Consumer`](xref:BenchmarkDotNet.Engines.Consumer) instance, preferably store it in a field (to exclude the cost of creating Consumer from the benchmark itself) and pass it to `Consume` extension method.

**Do not call `.ToArray()` because it's an expensive operation and it might dominate given benchmark!**

### Source code

[!code-csharp[IntroDeferredExecution.cs](../../../samples/BenchmarkDotNet.Samples/IntroDeferredExecution.cs)]

### Links

* The permanent link to this sample: @BenchmarkDotNet.Samples.IntroDeferredExecution

---