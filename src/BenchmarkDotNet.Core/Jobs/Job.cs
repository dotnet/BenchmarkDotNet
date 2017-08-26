using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Jobs
{
    public sealed class Job : JobMode<Job>
    {
        public static readonly Characteristic<EnvMode> EnvCharacteristic = Characteristic.Create((Job j) => j.Env);
        public static readonly Characteristic<RunMode> RunCharacteristic = Characteristic.Create((Job j) => j.Run);
        public static readonly Characteristic<InfrastructureMode> InfrastructureCharacteristic = Characteristic.Create((Job j) => j.Infrastructure);
        public static readonly Characteristic<AccuracyMode> AccuracyCharacteristic = Characteristic.Create((Job j) => j.Accuracy);

        // Env
        public static readonly Job Clr = new Job(nameof(Clr), EnvMode.Clr).Freeze();
        public static readonly Job Core = new Job(nameof(Core), EnvMode.Core).Freeze();
        public static readonly Job Mono = new Job(nameof(Mono), EnvMode.Mono).Freeze();

        public static readonly Job LegacyJitX86 = new Job(nameof(LegacyJitX86), EnvMode.LegacyJitX86).Freeze();
        public static readonly Job LegacyJitX64 = new Job(nameof(LegacyJitX64), EnvMode.LegacyJitX64).Freeze();
        public static readonly Job RyuJitX64 = new Job(nameof(RyuJitX64), EnvMode.RyuJitX64).Freeze();
        public static readonly Job RyuJitX86 = new Job(nameof(RyuJitX86), EnvMode.RyuJitX86).Freeze();

        // Run
        public static readonly Job Dry = new Job(nameof(Dry), RunMode.Dry).Freeze();
        public static readonly Job ShortRun = new Job(nameof(ShortRun), RunMode.Short).Freeze();
        public static readonly Job MediumRun = new Job(nameof(MediumRun), RunMode.Medium).Freeze();
        public static readonly Job LongRun = new Job(nameof(LongRun), RunMode.Long).Freeze();
        public static readonly Job VeryLongRun = new Job(nameof(VeryLongRun), RunMode.VeryLong).Freeze();

        // Infrastructure
        public static readonly Job InProcess = new Job(nameof(InProcess), InfrastructureMode.InProcess);
        public static readonly Job InProcessDontLogOutput = new Job(nameof(InProcessDontLogOutput), InfrastructureMode.InProcessDontLogOutput);

        public Job() : this((string)null) { }

        public Job(string id) : base(id)
        {
            EnvCharacteristic[this] = new EnvMode();
            RunCharacteristic[this] = new RunMode();
            InfrastructureCharacteristic[this] = new InfrastructureMode();
            AccuracyCharacteristic[this] = new AccuracyMode();
        }

        public Job(CharacteristicObject other) : this((string)null, other)
        {
        }

        public Job(params CharacteristicObject[] others) : this((string)null, others)
        {
        }

        public Job(string id, CharacteristicObject other) : this(id)
        {
            Apply(other);
        }

        public Job(string id, params CharacteristicObject[] others) : this(id)
        {
            Apply(others);
        }

        public EnvMode Env => EnvCharacteristic[this];
        public RunMode Run => RunCharacteristic[this];
        public InfrastructureMode Infrastructure => InfrastructureCharacteristic[this];
        public AccuracyMode Accuracy => AccuracyCharacteristic[this];

        public string ResolvedId => HasValue(IdCharacteristic) ? Id : JobIdGenerator.GenerateRandomId(this);
        public string FolderInfo => ResolvedId;

        public string DisplayInfo
        {
            get
            {
                var props = ResolveId(this, null);
                return props == IdCharacteristic.FallbackValue
                    ? ResolvedId
                    : ResolvedId + $"({props})";
            }
        }
    }
}