**BenchmarkDotNet:** Lightweight .NET-framework for benchmarks.

You can install BenchmarkDotNet via [NuGet package](https://www.nuget.org/packages/BenchmarkDotNet/).

**Benchmarks:** Simple benchmark program runner.

    Usage: Benchmarks <programs-names> [<arguments>]
    Arguments:
      -a, --all
          Run all available programs
      -d, --details
          Show detailed results
      -rc=<n>, --result-count=<n>
          Result set iteration count
      -wc=<n>, --warmup-count=<n>
          WarmUp set default iteration count
      -mwc=<n>, --max-warmup-count=<n>
          WarmUp set max iteration count
      -mwe=<n>, --max-warmup-error=<n>
          Max permissible error (in percent) as condition for finishing of WarmUp
      -pb=<false|true>, --print-benchmark=<false|true>
          Printing the report of each benchmark to the console
      -pa=<n>, --processor-affinity=<n>
          ProcessorAffinity
      -dw, --disable-warmup
          Disable WarmUp, equivalent of -mwc=0
      -s, --single
          Single result benchmark without WarmUp, equivalent of -mwc=0 -rc=1
      -of=<filename>, --output-file=<filename>
          Save results of benchmark competition to file

Benchmark program example:

    public class IncrementProgram
    {
        public void Run()
        {
            var competition = new BenchmarkCompetition();
            competition.AddTask("i++", () => After());
            competition.AddTask("++i", () => Before());
            competition.Run();
        }

        private const int IterationCount = 2000000000;

        public static int After()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; i++)
                counter++;
            return counter;
        }

        public static int Before()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; ++i)
                ++counter;
            return counter;
        }
    }

Usage example:

    Benchmarks.exe Increment --details

Result example:

    Target program: Increment
    BenchmarkCompetition: start

    ***** i++: start *****
    WarmUp:
    Ticks: 495926 ms: 231
    Ticks: 494928 ms: 230
    Ticks: 495826 ms: 231
    Ticks: 495682 ms: 231
    Ticks: 495454 ms: 231
    TickStats: Min=494928, Max=495926, Med=495682, StdDev=355, Err=00.20%
    MsStats: Min=230, Max=231, Med=231, StdDev=0.40

    Result:
    Ticks: 496039 ms: 231
    Ticks: 494509 ms: 230
    Ticks: 495990 ms: 231
    Ticks: 497172 ms: 231
    Ticks: 498618 ms: 232
    Ticks: 496355 ms: 231
    Ticks: 494009 ms: 230
    Ticks: 494197 ms: 230
    Ticks: 494269 ms: 230
    Ticks: 494556 ms: 230
    TickStats: Min=494009, Max=498618, Med=495273, StdDev=1450, Err=00.93%
    MsStats: Min=230, Max=232, Med=230, StdDev=0.66
    ***** i++: end *****

    ***** ++i: start *****
    WarmUp:
    Ticks: 497367 ms: 232
    Ticks: 494100 ms: 230
    Ticks: 494443 ms: 230
    Ticks: 495531 ms: 231
    Ticks: 494104 ms: 230
    TickStats: Min=494100, Max=497367, Med=494443, StdDev=1245, Err=00.66%
    MsStats: Min=230, Max=232, Med=230, StdDev=0.80

    Result:
    Ticks: 521387 ms: 243
    Ticks: 494761 ms: 230
    Ticks: 493923 ms: 230
    Ticks: 495095 ms: 230
    Ticks: 495012 ms: 230
    Ticks: 495339 ms: 231
    Ticks: 495084 ms: 230
    Ticks: 496295 ms: 231
    Ticks: 496313 ms: 231
    Ticks: 496413 ms: 231
    TickStats: Min=493923, Max=521387, Med=495217, StdDev=7844, Err=05.56%
    MsStats: Min=230, Max=243, Med=230, StdDev=3.80
    ***** ++i: end *****

    BenchmarkCompetition: finish

    Competition results:
    i++ : 230ms 495273 ticks [Error = 00.93%, StdDev = 0.66]
    ++i : 230ms 495217 ticks [Error = 05.56%, StdDev = 3.80]