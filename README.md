**BenchmarkDotNet:** Light .NET-framework for benchmarks.

**Benchmarks:** Simple benchmark program runner.

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

    Benchmarks.exe Increment

Result example:

	Target program: Increment
	BenchmarkCompetition: start

	***** i++: start *****
	WarmUp:
	Ticks: 1211858 ms: 565
	Ticks: 1215415 ms: 567
	Ticks: 1210399 ms: 564
	Ticks: 1117163 ms: 521
	Ticks: 988433 ms: 461
	Ticks: 924355 ms: 431
	Ticks: 848442 ms: 395
	Ticks: 762970 ms: 355
	Ticks: 734396 ms: 342
	Ticks: 689765 ms: 321
	Ticks: 590661 ms: 275
	Ticks: 499016 ms: 232
	Ticks: 499127 ms: 232
	Ticks: 498824 ms: 232
	Ticks: 498928 ms: 232
	Ticks: 499227 ms: 232
	TickStats: Min=498824, Max=1215415, Median=748683, Diff=143.66%
	MsStats: Min=232, Max=567, Median=348

	Result:
	Ticks: 498641 ms: 232
	Ticks: 499746 ms: 233
	Ticks: 499539 ms: 233
	Ticks: 499281 ms: 232
	Ticks: 499347 ms: 232
	Ticks: 498847 ms: 232
	Ticks: 499387 ms: 232
	Ticks: 499056 ms: 232
	Ticks: 499318 ms: 232
	Ticks: 498526 ms: 232
	TickStats: Min=498526, Max=499746, Median=499299, Diff=00.24%
	MsStats: Min=232, Max=233, Median=232
	***** i++: end *****

	***** ++i: start *****
	WarmUp:
	Ticks: 500325 ms: 233
	Ticks: 498858 ms: 232
	Ticks: 499322 ms: 232
	Ticks: 499575 ms: 233
	Ticks: 499512 ms: 233
	TickStats: Min=498858, Max=500325, Median=499512, Diff=00.29%
	MsStats: Min=232, Max=233, Median=233

	Result:
	Ticks: 499320 ms: 232
	Ticks: 498804 ms: 232
	Ticks: 499200 ms: 232
	Ticks: 499060 ms: 232
	Ticks: 499333 ms: 232
	Ticks: 499655 ms: 233
	Ticks: 498957 ms: 232
	Ticks: 500053 ms: 233
	Ticks: 499350 ms: 232
	Ticks: 499241 ms: 232
	TickStats: Min=498804, Max=500053, Median=499280, Diff=00.25%
	MsStats: Min=232, Max=233, Median=232
	***** ++i: end *****

	BenchmarkCompetition: finish

	Competition results:
	i++ : 232ms [Error: 00.24%]
	++i : 232ms [Error: 00.25%]