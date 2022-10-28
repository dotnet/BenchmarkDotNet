using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Jobs
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class RunMode : JobMode<RunMode>
    {
        public static readonly Characteristic<RunStrategy> RunStrategyCharacteristic = Characteristic.Create<RunMode, RunStrategy>(nameof(RunStrategy), RunStrategy.Throughput);

        public static readonly Characteristic<int> LaunchCountCharacteristic = CreateCharacteristic<int>(nameof(LaunchCount));
        public static readonly Characteristic<long> InvocationCountCharacteristic = CreateCharacteristic<long>(nameof(InvocationCount));
        public static readonly Characteristic<int> UnrollFactorCharacteristic = CreateCharacteristic<int>(nameof(UnrollFactor));
        public static readonly Characteristic<int> IterationCountCharacteristic = CreateCharacteristic<int>(nameof(IterationCount));
        public static readonly Characteristic<int> MinIterationCountCharacteristic = CreateCharacteristic<int>(nameof(MinIterationCount));
        public static readonly Characteristic<int> MaxIterationCountCharacteristic = CreateCharacteristic<int>(nameof(MaxIterationCount));
        public static readonly Characteristic<TimeInterval> IterationTimeCharacteristic = CreateCharacteristic<TimeInterval>(nameof(IterationTime));
        public static readonly Characteristic<int> WarmupCountCharacteristic = CreateCharacteristic<int>(nameof(WarmupCount));
        public static readonly Characteristic<int> MinWarmupIterationCountCharacteristic = CreateCharacteristic<int>(nameof(MinWarmupIterationCount));
        public static readonly Characteristic<int> MaxWarmupIterationCountCharacteristic = CreateCharacteristic<int>(nameof(MaxWarmupIterationCount));
        public static readonly Characteristic<bool> MemoryRandomizationCharacteristic = CreateCharacteristic<bool>(nameof(MemoryRandomization));

        public static readonly RunMode Dry = new RunMode(nameof(Dry))
        {
            LaunchCount = 1,
            WarmupCount = 1,
            IterationCount = 1,
            RunStrategy = RunStrategy.ColdStart,
            UnrollFactor = 1
        }.Freeze();

        public static readonly RunMode Short = new RunMode(nameof(Short))
        {
            LaunchCount = 1,
            WarmupCount = 3,
            IterationCount = 3
        }.Freeze();

        public static readonly RunMode Medium = new RunMode(nameof(Medium))
        {
            LaunchCount = 2,
            WarmupCount = 10,
            IterationCount = 15
        }.Freeze();

        public static readonly RunMode Long = new RunMode(nameof(Long))
        {
            LaunchCount = 3,
            WarmupCount = 15,
            IterationCount = 100
        }.Freeze();

        public static readonly RunMode VeryLong = new RunMode(nameof(VeryLong))
        {
            LaunchCount = 4,
            WarmupCount = 30,
            IterationCount = 500
        }.Freeze();


        public RunMode() : this(null)
        {
        }

        private RunMode(string id) : base(id)
        {
        }

        /// <summary>
        /// Available values: Throughput and ColdStart.
        ///     Throughput: default strategy which allows to get good precision level.
        ///     ColdStart: should be used only for measuring cold start of the application or testing purpose.
        ///     Monitoring: no overhead evaluating, with several target iterations. Perfect for macrobenchmarks without a steady state with high variance.
        /// </summary>
        public RunStrategy RunStrategy
        {
            get { return RunStrategyCharacteristic[this]; }
            set { RunStrategyCharacteristic[this] = value; }
        }

        /// <summary>
        /// How many times we should launch process with target benchmark.
        /// </summary>
        public int LaunchCount
        {
            get { return LaunchCountCharacteristic[this]; }
            set { LaunchCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// How many warmup iterations should be performed.
        /// </summary>
        public int WarmupCount
        {
            get { return WarmupCountCharacteristic[this]; }
            set { WarmupCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// How many target iterations should be performed
        /// If specified, <see cref="MinIterationCount"/> will be ignored.
        /// If specified, <see cref="MaxIterationCount"/> will be ignored.
        /// </summary>
        public int IterationCount
        {
            get { return IterationCountCharacteristic[this]; }
            set { IterationCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// Desired time of execution of an iteration. Used by Pilot stage to estimate the number of invocations per iteration.
        /// The default value is 500 milliseconds.
        /// </summary>
        public TimeInterval IterationTime
        {
            get { return IterationTimeCharacteristic[this]; }
            set { IterationTimeCharacteristic[this] = value; }
        }

        /// <summary>
        /// Invocation count in a single iteration.
        /// If specified, <see cref="IterationTime"/> will be ignored.
        /// If specified, it must be a multiple of <see cref="UnrollFactor"/>.
        /// </summary>
        public long InvocationCount
        {
            get { return InvocationCountCharacteristic[this]; }
            set { InvocationCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// How many times the benchmark method will be invoked per one iteration of a generated loop.
        /// </summary>
        public int UnrollFactor
        {
            get { return UnrollFactorCharacteristic[this]; }
            set { UnrollFactorCharacteristic[this] = value; }
        }

        /// <summary>
        /// Minimum count of target iterations that should be performed
        /// The default value is 15
        /// <remarks>If you set this value to below 15, then <see cref="MultimodalDistributionAnalyzer"/> is not going to work</remarks>
        /// </summary>
        public int MinIterationCount
        {
            get { return MinIterationCountCharacteristic[this]; }
            set { MinIterationCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// Maximum count of target iterations that should be performed
        /// The default value is 100
        /// <remarks>If you set this value to below 15, then <see cref="MultimodalDistributionAnalyzer"/>  is not going to work</remarks>
        /// </summary>
        public int MaxIterationCount
        {
            get { return MaxIterationCountCharacteristic[this]; }
            set { MaxIterationCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// Minimum count of warmup iterations that should be performed
        /// The default value is 6
        /// </summary>
        public int MinWarmupIterationCount
        {
            get { return MinWarmupIterationCountCharacteristic[this]; }
            set { MinWarmupIterationCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// Maximum count of warmup iterations that should be performed
        /// The default value is 50
        /// </summary>
        public int MaxWarmupIterationCount
        {
            get { return MaxWarmupIterationCountCharacteristic[this]; }
            set { MaxWarmupIterationCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// specifies whether Engine should allocate some random-sized memory between iterations
        /// <remarks>it makes [GlobalCleanup] and [GlobalSetup] methods to be executed after every iteration</remarks>
        /// </summary>
        public bool MemoryRandomization
        {
            get => MemoryRandomizationCharacteristic[this];
            set => MemoryRandomizationCharacteristic[this] = value;
        }
    }
}