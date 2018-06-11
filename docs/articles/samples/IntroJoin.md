---
uid: BenchmarkDotNet.Samples.IntroJoin
---

## Sample: IntroJoin

If you are using `BenchmarkSwitcher` and want to run all the benchmarks with a category from all types and join them into one summary table, use the `--join` option (or `BenchmarkSwitcher.RunAllJoined`): 

### Source code

[!code-csharp[IntroJoin.cs](../../../samples/BenchmarkDotNet.Samples/IntroJoin.cs)]

### Command line

```
--join --category=IntroJoinA
```

### Output

```markdown
|       Type | Method |     Mean | Error |
|----------- |------- |---------:|------:|
| IntroJoin1 |      A | 10.99 ms |    NA |
| IntroJoin2 |      A | 12.50 ms |    NA |
```

### See also

* @docs.filters