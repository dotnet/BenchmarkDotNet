using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Horology;

// ReSharper disable once CheckNamespace

namespace BenchmarkDotNet.Jobs
{
    public sealed class RunMode : JobMode<RunMode>
    {
        public static readonly Characteristic<RunStrategy> RunStrategyCharacteristic = Characteristic.Create<RunMode, RunStrategy>(nameof(RunStrategy), RunStrategy.Throughput);

        public static readonly Characteristic<int> LaunchCountCharacteristic = CreateCharacteristic<int>(nameof(LaunchCount));
        public static readonly Characteristic<int> WarmupCountCharacteristic = CreateCharacteristic<int>(nameof(WarmupCount));
        public static readonly Characteristic<int> TargetCountCharacteristic = CreateCharacteristic<int>(nameof(TargetCount));
        public static readonly Characteristic<TimeInterval> IterationTimeCharacteristic = CreateCharacteristic<TimeInterval>(nameof(IterationTime));
        public static readonly Characteristic<int> InvocationCountCharacteristic = CreateCharacteristic<int>(nameof(InvocationCount));
        public static readonly Characteristic<int> UnrollFactorCharacteristic = CreateCharacteristic<int>(nameof(UnrollFactor));
        public static readonly Characteristic<int> MinTargetIterationCountCharacteristic = CreateCharacteristic<int>(nameof(MinTargetIterationCount));
        public static readonly Characteristic<int> MaxTargetIterationCountCharacteristic = CreateCharacteristic<int>(nameof(MaxTargetIterationCount));

        public static readonly RunMode Dry = new RunMode(nameof(Dry))
        {
            LaunchCount = 1,
            WarmupCount = 1,
            TargetCount = 1,
            RunStrategy = RunStrategy.ColdStart,
            UnrollFactor = 1
        }.Freeze();

        public static readonly RunMode Short = new RunMode(nameof(Short))
        {
            LaunchCount = 1,
            WarmupCount = 3,
            TargetCount = 3
        }.Freeze();

        public static readonly RunMode Medium = new RunMode(nameof(Medium))
        {
            LaunchCount = 2,
            WarmupCount = 10,
            TargetCount = 15
        }.Freeze();

        public static readonly RunMode Long = new RunMode(nameof(Long))
        {
            LaunchCount = 3,
            WarmupCount = 15,
            TargetCount = 100
        }.Freeze();

        public static readonly RunMode VeryLong = new RunMode(nameof(VeryLong))
        {
            LaunchCount = 4,
            WarmupCount = 30,
            TargetCount = 500
        }.Freeze();


        public RunMode() : this(null)
        {
        }

        public RunMode(string id) : base(id)
        {
        }

        /// <summary>
        /// Available values: Throughput and ColdStart.
        ///     Throughput: default strategy which allows to get good precision level.
        ///     ColdStart: should be used only for measuring cold start of the application or testing purpose.
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
        /// If specified, <see cref="MinTargetIterationCount"/> will be ignored.
        /// If specified, <see cref="MaxTargetIterationCount"/> will be ignored.
        /// </summary>
        public int TargetCount
        {
            get { return TargetCountCharacteristic[this]; }
            set { TargetCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// Desired time of execution of an iteration.
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
        public int InvocationCount
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
        /// The default is 15
        /// </summary>
        public int MinTargetIterationCount
        {
            get { return MinTargetIterationCountCharacteristic[this]; }
            set { MinTargetIterationCountCharacteristic[this] = value; }
        }

        /// <summary>
        /// Maximum count of target iterations that should be performed
        /// The default is 100 
        /// </summary>
        public int MaxTargetIterationCount
        {
            get { return MaxTargetIterationCountCharacteristic[this]; }
            set { MaxTargetIterationCountCharacteristic[this] = value; }
        }
    }
}