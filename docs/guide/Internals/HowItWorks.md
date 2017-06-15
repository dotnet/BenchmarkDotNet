# How it works

BenchmarkDotNet follows the following steps to run your benchmarks:

1. `BenchmarkRunner` generates an isolated project per each benchmark method/job/params and builds it in Release mode.
2. Next, we take each method/job/params combination and try to measure its performance by launching benchmark process several times (`LaunchCount`).
3. An invocation of the target method is an *operation*. A bunch of operation is an *iteration*. If you have an `IterationSetup` method, it will be invoked before each iteration, 
but not between operations. We have the following type of iterations:
    * `Pilot`: The best operation count will be chosen.
    * `IdleWarmup`, `IdleTarget`: BenchmarkDotNet overhead will be evaluated.
    * `MainWarmup`: Warmup of the main method.
    * `MainTarget`: Main measurements.
    * `Result` = `MainTarget` - `<AverageOverhead>`
4. After all of the measurements, BenchmarkDotNet creates:
    * An instance of the `Summary` class that contains all information about benchmark runs.
    * A set of files that contains summary in human-readable and machine-readable formats.
    * A set of plots.
