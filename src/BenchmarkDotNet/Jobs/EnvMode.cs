using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Jobs
{
    public sealed class EnvMode : JobMode<EnvMode>
    {
        public static readonly Characteristic<Platform> PlatformCharacteristic = CreateCharacteristic<Platform>(nameof(Platform));
        public static readonly Characteristic<Jit> JitCharacteristic = CreateCharacteristic<Jit>(nameof(Jit));
        public static readonly Characteristic<Runtime> RuntimeCharacteristic = CreateCharacteristic<Runtime>(nameof(Runtime));
        public static readonly Characteristic<IntPtr> AffinityCharacteristic = CreateCharacteristic<IntPtr>(nameof(Affinity));
        public static readonly Characteristic<GcMode> GcCharacteristic = CreateCharacteristic<GcMode>(nameof(Gc));

        public static readonly EnvMode Clr = new EnvMode(Runtime.Clr).Freeze();
        public static readonly EnvMode Core = new EnvMode(Runtime.Core).Freeze();
        public static readonly EnvMode Mono = new EnvMode(Runtime.Mono).Freeze();
        public static readonly EnvMode CoreRT = new EnvMode(Runtime.CoreRT).Freeze();
        public static readonly EnvMode LegacyJitX86 = new EnvMode(nameof(LegacyJitX86), Jit.LegacyJit, Platform.X86).Freeze();
        public static readonly EnvMode LegacyJitX64 = new EnvMode(nameof(LegacyJitX64), Jit.LegacyJit, Platform.X64).Freeze();
        public static readonly EnvMode RyuJitX64 = new EnvMode(nameof(RyuJitX64), Jit.RyuJit, Platform.X64).Freeze();
        public static readonly EnvMode RyuJitX86 = new EnvMode(nameof(RyuJitX86), Jit.RyuJit, Platform.X86).Freeze();

        public EnvMode() : this(id: null) { }

        public EnvMode(Runtime runtime) : this(runtime.ToString())
        {
            Runtime = runtime;
        }

        public EnvMode(string id, Jit jit, Platform platform) : this(id)
        {
            Jit = jit;
            Platform = platform;
            if (jit == Jit.LegacyJit)
                Runtime = Runtime.Clr;
        }

        public EnvMode(string id) : base(id)
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
    }
}