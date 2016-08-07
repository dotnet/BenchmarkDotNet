# FAQ

**Question** Benchmarks takes a lot of time, how I can speedup it?

**Answer** In general case, you need a lot of time for achieving good accuracy. If you are sure that you don't have any tricky performance effects 
and you don't need such level of accuracy, you can create a special Job. An example:

```cs
public class FastAndDirtyConfig : ManualConfig
{
    public FastAndDirtyConfig()
    {
        Add(Job.Default
            .WithLaunchCount(1)     // benchmark process will be launched only once
            .WithIterationTime(100) // 100ms per iteration
            .WithWarmupCount(3)     // 3 warmup iteration
            .WithTargetCount(3)     // 3 target iteration
        );
    }
}
```
