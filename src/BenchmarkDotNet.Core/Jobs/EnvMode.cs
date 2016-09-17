using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Jobs
{
    public sealed class EnvMode
    {
        public static readonly EnvMode Default = new EnvMode();

        public static readonly JobMutator Clr = CreateMutator(Environments.Runtime.Clr);
        public static readonly JobMutator Core = CreateMutator(Environments.Runtime.Core);
        public static readonly JobMutator Mono = CreateMutator(Environments.Runtime.Mono);

        public static readonly JobMutator LegacyJitX86 = CreateMutator(Environments.Jit.LegacyJit, Environments.Platform.X86);
        public static readonly JobMutator LegacyJitX64 = CreateMutator(Environments.Jit.LegacyJit, Environments.Platform.X64);
        public static readonly JobMutator RyuJitX64 = CreateMutator(Environments.Jit.RyuJit, Environments.Platform.X64);

        private EnvMode()
        {
        }

        private static ICharacteristic<T> Create<T>(string id) => Characteristic<T>.Create("Env", id);

        /// <summary>
        /// Platform (x86 or x64)
        /// </summary>
        public ICharacteristic<Platform> Platform { get; private set; } = Create<Platform>(nameof(Platform));

        /// <summary>
        /// JIT (Just-In-Time compiler)
        /// </summary>
        public ICharacteristic<Jit> Jit { get; private set; } = Create<Jit>(nameof(Jit));

        /// <summary>
        /// Runtime
        /// </summary>
        public ICharacteristic<Runtime> Runtime { get; private set; } = Create<Runtime>(nameof(Runtime));

        /// <summary>
        /// ProcessorAffinity for the benchmark process.
        /// See also: https://msdn.microsoft.com/library/system.diagnostics.process.processoraffinity.aspx
        /// </summary>
        public ICharacteristic<IntPtr> Affinity { get; private set; } = Create<IntPtr>(nameof(Affinity));

        /// <summary>
        /// GcMode
        /// </summary>
        public GcMode Gc { get; private set; } = GcMode.Default;

        public static JobMutator CreateMutator(Runtime runtime)
        {
            return new JobMutator(runtime.ToString()).Add(Default.Runtime.Mutate(runtime));
        }

        public static JobMutator CreateMutator(Jit jit, Platform platform)
        {
            var mutator = new JobMutator(jit.ToString() + platform).
                Add(Default.Jit.Mutate(jit)).
                Add(Default.Platform.Mutate(platform));
            if (jit == Environments.Jit.LegacyJit)
                mutator = mutator.Add(Default.Runtime.Mutate(Environments.Runtime.Clr));
            return mutator;
        }

        public static EnvMode Parse(CharacteristicSet set)
        {
            var mode = new EnvMode();
            mode.Platform = mode.Platform.Mutate(set);
            mode.Jit = mode.Jit.Mutate(set);
            mode.Runtime = mode.Runtime.Mutate(set);
            mode.Affinity = mode.Affinity.Mutate(set);
            mode.Gc = GcMode.Parse(set);
            return mode;
        }

        public CharacteristicSet ToSet() => new CharacteristicSet(
            Platform,
            Jit,
            Runtime,
            Affinity
        ).Mutate(Gc.ToSet());
    }
}