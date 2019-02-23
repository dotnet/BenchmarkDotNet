using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Jobs
{
    public sealed class EnvironmentMode : JobMode<EnvironmentMode>
    {
        public static readonly Characteristic<Platform> PlatformCharacteristic = CreateCharacteristic<Platform>(nameof(Platform));
        public static readonly Characteristic<Jit> JitCharacteristic = CreateCharacteristic<Jit>(nameof(Jit));
        public static readonly Characteristic<Runtime> RuntimeCharacteristic = CreateCharacteristic<Runtime>(nameof(Runtime));
        public static readonly Characteristic<IntPtr> AffinityCharacteristic = CreateCharacteristic<IntPtr>(nameof(Affinity));
        public static readonly Characteristic<GcMode> GcCharacteristic = CreateCharacteristic<GcMode>(nameof(Gc));
        public static readonly Characteristic<IReadOnlyList<EnvironmentVariable>> EnvironmentVariablesCharacteristic = CreateCharacteristic<IReadOnlyList<EnvironmentVariable>>(nameof(EnvironmentVariables));

        public static readonly EnvironmentMode Clr = new EnvironmentMode(Runtime.Clr).Freeze();
        public static readonly EnvironmentMode Core = new EnvironmentMode(Runtime.Core).Freeze();
        public static readonly EnvironmentMode Mono = new EnvironmentMode(Runtime.Mono).Freeze();
        public static readonly EnvironmentMode CoreRT = new EnvironmentMode(Runtime.CoreRT).Freeze();
        public static readonly EnvironmentMode LegacyJitX86 = new EnvironmentMode(nameof(LegacyJitX86), Jit.LegacyJit, Platform.X86).Freeze();
        public static readonly EnvironmentMode LegacyJitX64 = new EnvironmentMode(nameof(LegacyJitX64), Jit.LegacyJit, Platform.X64).Freeze();
        public static readonly EnvironmentMode RyuJitX64 = new EnvironmentMode(nameof(RyuJitX64), Jit.RyuJit, Platform.X64).Freeze();
        public static readonly EnvironmentMode RyuJitX86 = new EnvironmentMode(nameof(RyuJitX86), Jit.RyuJit, Platform.X86).Freeze();

        [PublicAPI] public EnvironmentMode() : this(id: null) { }

        [PublicAPI] public EnvironmentMode(Runtime runtime) : this(runtime.ToString())
        {
            Runtime = runtime;
        }

        [PublicAPI] public EnvironmentMode(string id, Jit jit, Platform platform) : this(id)
        {
            Jit = jit;
            Platform = platform;
            if (jit == Jit.LegacyJit)
                Runtime = Runtime.Clr;
        }

        [PublicAPI] public EnvironmentMode(string id) : base(id)
        {
            GcCharacteristic[this] = new GcMode();
        }

        /// <summary>
        /// Platform (x86 or x64)
        /// </summary>
        public Platform Platform
        {
            get { return PlatformCharacteristic[this]; }
            set { PlatformCharacteristic[this] = value; }
        }

        /// <summary>
        /// JIT (Just-In-Time compiler)
        /// </summary>
        public Jit Jit
        {
            get { return JitCharacteristic[this]; }
            set { JitCharacteristic[this] = value; }
        }

        /// <summary>
        /// Runtime
        /// </summary>
        public Runtime Runtime
        {
            get { return RuntimeCharacteristic[this]; }
            set { RuntimeCharacteristic[this] = value; }
        }

        /// <summary>
        /// ProcessorAffinity for the benchmark process.
        /// See also: https://msdn.microsoft.com/library/system.diagnostics.process.processoraffinity.aspx
        /// </summary>
        public IntPtr Affinity
        {
            get { return AffinityCharacteristic[this]; }
            set { AffinityCharacteristic[this] = value; }
        }

        /// <summary>
        /// GcMode
        /// </summary>
        public GcMode Gc => GcCharacteristic[this];
        
        public IReadOnlyList<EnvironmentVariable> EnvironmentVariables
        {
            get => EnvironmentVariablesCharacteristic[this];
            set => EnvironmentVariablesCharacteristic[this] = value;
        }

        /// <summary>
        /// Adds the specified <paramref name="variable"/> to <see cref="EnvironmentVariables"/>.
        /// If <see cref="EnvironmentVariables"/> already contains a variable with the same key,
        /// it will be overriden.
        /// </summary>
        /// <param name="variable">The new environment variable which should be added to <see cref="EnvironmentVariables"/></param>
        public void SetEnvironmentVariable(EnvironmentVariable variable)
        {
            var newVariables = new List<EnvironmentVariable>();
            if (EnvironmentVariables != null)
                newVariables.AddRange(EnvironmentVariables);
            newVariables.RemoveAll(v => v.Key.Equals(variable.Key, StringComparison.Ordinal));
            newVariables.Add(variable);
            EnvironmentVariables = newVariables;
        }
    }
}