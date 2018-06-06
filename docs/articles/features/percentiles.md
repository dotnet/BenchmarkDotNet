# Percentiles

The percentile represents a higher boundary for specified percentage of the measurements.
For example, 95th percentile = 500ms means that 95% of all samples are not slower than 500ms.
This metric is not very useful in microbenchmarks, as the values from consequent runs have a very narrow distribution.
However, real-world scenarios often have so-called long tail distribution (due to IO delays, locks, memory access latency and so on), so the average execution time 
cannot be trusted.

The percentiles allow to include the tail of distribution into the comparison. However, it requires some preparations steps.
At first, you should have enough runs to count percentiles from. The `TargetCount` in the config should be set to 10-20 runs at least.  

Second, the count of iterations for each run should not be very high, or the peak timings will be averaged.
The `IterationTime = 25` works fine for most cases; for long-running benchmarks the `Mode = Mode.SingleRun` will be the best choice. 
However, feel free to experiment with the config values.

Third, if you want to be sure that measurements are repeatable, set the `LaunchCount` to 3 or higher.

And last, don't forget to include the columns into the config. They are not included by default (as said above, these are not too useful for most of the benchmarks).
There're predefined `StatisticColumn.P0`..`StatisticColumn.P100` for absolute timing percentiles and `BaselineDiffColumn.Scaled50`..`BaselineDiffColumn.Scaled95` 
for relative percentiles.

##Example

Run the IntroPercentiles sample. It contains three benchmark methods.

* First delays for 20 ms constantly.
* The second has random delays for 10..30 ms.
* And the third delays for 10ms 85 times of 100 and delays for 40ms 15 times of 100.

Here's the output from the benchmark (some columns removed for brevity):

         Method |     Median |     StdDev | Scaled |         P0 |        P50 |        P80 |        P85 |        P95 |       P100 | ScaledP50 | ScaledP85 | ScaledP95
--------------- |----------- |----------- |------- |----------- |----------- |----------- |----------- |----------- |----------- |---------- |---------- |----------
 ConstantDelays | 20.3813 ms |  0.2051 ms |   1.00 | 20.0272 ms | 20.3813 ms | 20.4895 ms | 20.4954 ms | 20.5869 ms | 21.1471 ms |      1.00 |      1.00 |      1.00
   RandomDelays | 19.8055 ms |  5.7556 ms |   0.97 | 10.0793 ms | 19.8055 ms | 25.4173 ms | 26.5187 ms | 29.0313 ms | 29.4550 ms |      0.97 |      1.29 |      1.41
     RareDelays | 10.3385 ms | 11.4828 ms |   0.51 | 10.0157 ms | 10.3385 ms | 10.5211 ms | 40.0560 ms | 40.3992 ms | 40.4674 ms |      0.51 |      1.95 |      1.96

Note that the 'Scaled' column kinda lies to you. 
The "almost same" RandomDelays method is actually not so performant and the seems-to-be-fastest RareDelays method is 2 times slower 15 times of 100.

Also, it's very easy to screw the results with incorrect setup. For example, the same code being run with

```cs
new Job
{
	TargetCount = 5,
	IterationTime = 500
}
```

completely hides the peak values:

         Method |     Median |    StdDev | Scaled |         P0 |        P50 |        P80 |        P85 |        P95 |       P100 | ScaledP50 | ScaledP85 | ScaledP95
--------------- |----------- |---------- |------- |----------- |----------- |----------- |----------- |----------- |----------- |---------- |---------- |----------
 ConstantDelays | 20.2692 ms | 0.0308 ms |   1.00 | 20.1986 ms | 20.2692 ms | 20.2843 ms | 20.2968 ms | 20.3097 ms | 20.3122 ms |      1.00 |      1.00 |      1.00
   RandomDelays | 18.9965 ms | 0.8601 ms |   0.94 | 18.1339 ms | 18.9965 ms | 19.8126 ms | 19.8278 ms | 20.4485 ms | 20.9466 ms |      0.94 |      0.98 |      1.01
     RareDelays | 14.0912 ms | 2.8619 ms |   0.70 | 10.2606 ms | 14.0912 ms | 15.7653 ms | 17.3862 ms | 18.6728 ms | 18.6940 ms |      0.70 |      0.86 |      0.92
