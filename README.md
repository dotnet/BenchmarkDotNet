**BenchmarkDotNet:** Lightweight .NET-framework for benchmarks.

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

    Benchmarks.exe Increment --details

Result example:

	Target program: Increment
	BenchmarkCompetition: start
	
	***** i++: start *****
	WarmUp:
	Ticks: 1214468 ms: 566
	Ticks: 1213456 ms: 566
	Ticks: 1197767 ms: 558
	Ticks: 1118524 ms: 521
	Ticks: 972896 ms: 453
	Ticks: 890580 ms: 415
	Ticks: 836877 ms: 390
	Ticks: 766459 ms: 357
	Ticks: 728634 ms: 339
	Ticks: 693158 ms: 323
	Ticks: 562414 ms: 262
	Ticks: 500549 ms: 233
	Ticks: 499788 ms: 233
	Ticks: 499223 ms: 232
	Ticks: 500034 ms: 233
	Ticks: 500401 ms: 233
	TickStats: Min=499223, Max=1214468, Med=747546, StdDev=269868, Err=143.27%
	MsStats: Min=232, Max=566, Med=348, StdDev=125.85
	
	Result:
	Ticks: 499911 ms: 233
	Ticks: 499133 ms: 232
	Ticks: 499636 ms: 233
	Ticks: 499720 ms: 233
	Ticks: 499894 ms: 233
	Ticks: 500764 ms: 233
	Ticks: 501977 ms: 234
	Ticks: 501319 ms: 233
	Ticks: 499859 ms: 233
	Ticks: 499710 ms: 233
	TickStats: Min=499133, Max=501977, Med=499876, StdDev=834, Err=00.57%
	MsStats: Min=232, Max=234, Med=233, StdDev=0.45
	***** i++: end *****
	
	***** ++i: start *****
	WarmUp:
	Ticks: 500340 ms: 233
	Ticks: 499891 ms: 233
	Ticks: 500608 ms: 233
	Ticks: 500135 ms: 233
	Ticks: 499577 ms: 233
	TickStats: Min=499577, Max=500608, Med=500135, StdDev=356, Err=00.21%
	MsStats: Min=233, Max=233, Med=233, StdDev=0.00
	
	Result:
	Ticks: 499572 ms: 233
	Ticks: 499127 ms: 232
	Ticks: 499461 ms: 233
	Ticks: 499470 ms: 233
	Ticks: 499350 ms: 232
	Ticks: 499199 ms: 232
	Ticks: 499223 ms: 232
	Ticks: 499133 ms: 232
	Ticks: 499168 ms: 232
	Ticks: 500137 ms: 233
	TickStats: Min=499127, Max=500137, Med=499286, StdDev=292, Err=00.20%
	MsStats: Min=232, Max=233, Med=232, StdDev=0.49
	***** ++i: end *****
	
	BenchmarkCompetition: finish
	
	Competition results:
	i++ : 233ms [Error = 00.57%, StdDev = 0.45]
	++i : 232ms [Error = 00.20%, StdDev = 0.49]