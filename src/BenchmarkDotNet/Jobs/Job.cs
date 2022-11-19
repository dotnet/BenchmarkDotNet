using BenchmarkDotNet.Characteristics;
using JetBrains.Annotations;
using System;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Jobs
{
    [SuppressMessage("ReSharper", "UnassignedReadonlyField")]
    public sealed class Job : JobMode<Job>
    {
        [PublicAPI] public static readonly Characteristic<EnvironmentMode> EnvironmentCharacteristic = CreateCharacteristic<EnvironmentMode>(nameof(Environment));
        [PublicAPI] public static readonly Characteristic<RunMode> RunCharacteristic = CreateCharacteristic<RunMode>(nameof(Run));
        [PublicAPI] public static readonly Characteristic<InfrastructureMode> InfrastructureCharacteristic = CreateCharacteristic<InfrastructureMode>(nameof(Infrastructure));
        [PublicAPI] public static readonly Characteristic<AccuracyMode> AccuracyCharacteristic = CreateCharacteristic<AccuracyMode>(nameof(Accuracy));
        [PublicAPI] public static readonly Characteristic<MetaMode> MetaCharacteristic = CreateCharacteristic<MetaMode>(nameof(Meta));

        internal static readonly Characteristic<bool> ImplicitIdCharacteristic = CreateHiddenCharacteristic<bool>("ImplicitId");

        public static readonly Job LegacyJitX86 = new Job(EnvironmentMode.LegacyJitX86).WithImplicitId(nameof(LegacyJitX86)).Freeze();
        public static readonly Job LegacyJitX64 = new Job(EnvironmentMode.LegacyJitX64).WithImplicitId(nameof(LegacyJitX64)).Freeze();
        public static readonly Job RyuJitX64 = new Job(EnvironmentMode.RyuJitX64).WithImplicitId(nameof(RyuJitX64)).Freeze();
        public static readonly Job RyuJitX86 = new Job(EnvironmentMode.RyuJitX86).WithImplicitId(nameof(RyuJitX86)).Freeze();

        // Run
        public static readonly Job Dry = new Job(RunMode.Dry).WithImplicitId(nameof(Dry)).Freeze();

        public static readonly Job ShortRun = new Job(RunMode.Short).WithImplicitId(nameof(ShortRun)).Freeze();
        public static readonly Job MediumRun = new Job(RunMode.Medium).WithImplicitId(nameof(MediumRun)).Freeze();
        public static readonly Job LongRun = new Job(RunMode.Long).WithImplicitId(nameof(LongRun)).Freeze();
        public static readonly Job VeryLongRun = new Job(RunMode.VeryLong).WithImplicitId(nameof(VeryLongRun)).Freeze();

        // Infrastructure
        public static readonly Job InProcess = new Job(InfrastructureMode.InProcess).WithImplicitId(nameof(InProcess));
        public static readonly Job InProcessDontLogOutput = new Job(InfrastructureMode.InProcessDontLogOutput).WithImplicitId(nameof(InProcessDontLogOutput));

        public Job() : this((string)null) { }

        public Job(string id) : base(id)
        {
            EnvironmentCharacteristic[this] = new EnvironmentMode();
            RunCharacteristic[this] = new RunMode();
            InfrastructureCharacteristic[this] = new InfrastructureMode();
            AccuracyCharacteristic[this] = new AccuracyMode();
            MetaCharacteristic[this] = new MetaMode();
            ImplicitIdCharacteristic[this] = false;
        }

        public Job(CharacteristicObject other) : this((string)null, other)
        {
        }

        public Job(params CharacteristicObject[] others) : this(null, others)
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

        public EnvironmentMode Environment => EnvironmentCharacteristic[this];
        public RunMode Run => RunCharacteristic[this];
        public InfrastructureMode Infrastructure => InfrastructureCharacteristic[this];
        public AccuracyMode Accuracy => AccuracyCharacteristic[this];
        public MetaMode Meta => MetaCharacteristic[this];

        public string ResolvedId => HasValue(IdCharacteristic) ? Id : JobIdGenerator.GenerateRandomId(this);
        public string FolderInfo => ResolvedId;

        public string DisplayInfo
        {
            get
            {
                string props = ResolveId(this, null);
                return props == IdCharacteristic.FallbackValue
                    ? ResolvedId
                    : ResolvedId + $"({props})";
            }
        }
    }
}