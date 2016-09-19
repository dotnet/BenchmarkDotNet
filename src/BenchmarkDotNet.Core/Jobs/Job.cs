using System.Linq;
using BenchmarkDotNet.Characteristics;

namespace BenchmarkDotNet.Jobs
{
    public sealed class Job
    {
        #region Predifined

        public static readonly Job Default = new Job().WithId("Default");

        // Env
        public static readonly Job Clr = Default.Mutate(EnvMode.Clr).WithId(nameof(Clr));
        public static readonly Job Core = Default.Mutate(EnvMode.Core).WithId(nameof(Core));
        public static readonly Job Mono = Default.Mutate(EnvMode.Mono).WithId(nameof(Mono));

        public static readonly Job LegacyJitX86 = Default.Mutate(EnvMode.LegacyJitX86).WithId(nameof(LegacyJitX86));
        public static readonly Job LegacyJitX64 = Default.Mutate(EnvMode.LegacyJitX64).WithId(nameof(LegacyJitX64));
        public static readonly Job RyuJitX64 = Default.Mutate(EnvMode.RyuJitX64).WithId(nameof(RyuJitX64));

        // Run
        public static readonly Job Dry = Default.Mutate(RunMode.Dry).WithId(nameof(Dry));
        public static readonly Job ShortRun = Default.Mutate(RunMode.Short).WithId(nameof(ShortRun));
        public static readonly Job MediumRun = Default.Mutate(RunMode.Medium).WithId(nameof(MediumRun));
        public static readonly Job LongRun = Default.Mutate(RunMode.Long).WithId(nameof(LongRun));
        public static readonly Job VeryLongRun = Default.Mutate(RunMode.VeryLong).WithId(nameof(VeryLongRun));

        #endregion

        public ICharacteristic<string> Id { get; private set; } = Characteristic<string>.Create("Job", "Id");
        public EnvMode Env { get; private set; } = EnvMode.Default;
        public RunMode Run { get; private set; } = RunMode.Default;
        public InfrastructureMode Infrastructure { get; private set; } = InfrastructureMode.Default;
        public AccuracyMode Accuracy { get; private set; } = AccuracyMode.Default;

        public static Job Parse(CharacteristicSet set, bool clearId = true)
        {
            var job = new Job();
            if (!clearId)
                job.Id = job.Id.Mutate(set);
            job.Env = EnvMode.Parse(set);
            job.Run = RunMode.Parse(set);
            job.Infrastructure = InfrastructureMode.Parse(set);
            job.Accuracy = AccuracyMode.Parse(set);
            return job;
        }

        public Job WithId(string id)
        {
            var job = Clone();
            job.Id = job.Id.Mutate(id);
            return job;
        }

        public Job Clone() => Parse(ToSet());

        public CharacteristicSet ToSet(bool includeId = true) =>
            new CharacteristicSet(includeId ? new ICharacteristic[] { Id } : new ICharacteristic[0]).Mutate(
                Env.ToSet(),
                Run.ToSet(),
                Infrastructure.ToSet(),
                Accuracy.ToSet()
            );

        public Job Mutate(JobMutator mutator) => mutator.Apply(this);

        public string ResolvedId => Id.IsDefault ? JobIdGenerator.GenerateRandomId(this) : Id.SpecifiedValue;
        public string FolderInfo => ResolvedId;

        public string DisplayInfo
        {
            get
            {
                var set = ToSet(false);
                string characteristics = set.AllAreDefaults() ? "" : "(" + CharacteristicSetPresenter.Display.ToPresentation(set) + ")";
                return ResolvedId + characteristics;
            }
        }
    }
}