namespace BenchmarkDotNet.Jobs
{
    /// <summary>
    /// Well-known runtime moniker strings for use with the job attributes (e.g. <c>[SimpleJob(RuntimeMoniker.Net80)]</c>)
    /// and the <c>--runtimes</c> command line option. Any moniker string accepted by <c>Runtime.Parse</c> can also be used
    /// directly, which allows targeting custom runtimes and arbitrary versions.
    /// </summary>
    public static class RuntimeMoniker
    {
        /// <summary>Legacy Mono</summary>
        public const string Mono = "mono";

        /// <summary>Legacy Mono AOT</summary>
        public const string MonoAot = "monoaot";

        /// <summary>.NET Framework 4.6.1</summary>
        public const string Net461 = "net461";
        /// <summary>.NET Framework 4.6.2</summary>
        public const string Net462 = "net462";
        /// <summary>.NET Framework 4.7</summary>
        public const string Net47 = "net47";
        /// <summary>.NET Framework 4.7.1</summary>
        public const string Net471 = "net471";
        /// <summary>.NET Framework 4.7.2</summary>
        public const string Net472 = "net472";
        /// <summary>.NET Framework 4.8</summary>
        public const string Net48 = "net48";
        /// <summary>.NET Framework 4.8.1</summary>
        public const string Net481 = "net481";

        /// <summary>.NET Core 2.0</summary>
        public const string NetCoreApp20 = "netcoreapp2.0";
        /// <summary>.NET Core 2.1</summary>
        public const string NetCoreApp21 = "netcoreapp2.1";
        /// <summary>.NET Core 2.2</summary>
        public const string NetCoreApp22 = "netcoreapp2.2";
        /// <summary>.NET Core 3.0</summary>
        public const string NetCoreApp30 = "netcoreapp3.0";
        /// <summary>.NET Core 3.1</summary>
        public const string NetCoreApp31 = "netcoreapp3.1";

        /// <summary>.NET 5.0</summary>
        public const string Net50 = "net5.0";
        /// <summary>.NET 6.0</summary>
        public const string Net60 = "net6.0";
        /// <summary>.NET 7.0</summary>
        public const string Net70 = "net7.0";
        /// <summary>.NET 8.0</summary>
        public const string Net80 = "net8.0";
        /// <summary>.NET 9.0</summary>
        public const string Net90 = "net9.0";
        /// <summary>.NET 10.0</summary>
        public const string Net10_0 = "net10.0";
        /// <summary>.NET 11.0</summary>
        public const string Net11_0 = "net11.0";

        /// <summary>NativeAOT compiled as net7.0</summary>
        public const string NativeAot70 = "nativeaot7.0";
        /// <summary>NativeAOT compiled as net8.0</summary>
        public const string NativeAot80 = "nativeaot8.0";
        /// <summary>NativeAOT compiled as net9.0</summary>
        public const string NativeAot90 = "nativeaot9.0";
        /// <summary>NativeAOT compiled as net10.0</summary>
        public const string NativeAot10_0 = "nativeaot10.0";
        /// <summary>NativeAOT compiled as net11.0</summary>
        public const string NativeAot11_0 = "nativeaot11.0";

        /// <summary>.NET 6 using MonoVM (not CLR which is the default)</summary>
        public const string Mono60 = "mono6.0";
        /// <summary>.NET 7 using MonoVM (not CLR which is the default)</summary>
        public const string Mono70 = "mono7.0";
        /// <summary>.NET 8 using MonoVM (not CLR which is the default)</summary>
        public const string Mono80 = "mono8.0";
        /// <summary>.NET 9 using MonoVM (not CLR which is the default)</summary>
        public const string Mono90 = "mono9.0";
        /// <summary>.NET 10 using MonoVM (not CLR which is the default)</summary>
        public const string Mono10_0 = "mono10.0";
        /// <summary>.NET 11 using MonoVM (not CLR which is the default)</summary>
        public const string Mono11_0 = "mono11.0";

        /// <summary>.NET 8 CLR with composite ReadyToRun compilation</summary>
        public const string R2R80 = "r2r8.0";
        /// <summary>.NET 9 CLR with composite ReadyToRun compilation</summary>
        public const string R2R90 = "r2r9.0";
        /// <summary>.NET 10 CLR with composite ReadyToRun compilation</summary>
        public const string R2R10_0 = "r2r10.0";
        /// <summary>.NET 11 CLR with composite ReadyToRun compilation</summary>
        public const string R2R11_0 = "r2r11.0";

        /// <summary>Mono WebAssembly with net8.0</summary>
        public const string MonoWasm80 = "monowasm8.0";
        /// <summary>Mono WebAssembly with net9.0</summary>
        public const string MonoWasm90 = "monowasm9.0";
        /// <summary>Mono WebAssembly with net10.0</summary>
        public const string MonoWasm10_0 = "monowasm10.0";
        /// <summary>Mono WebAssembly with net11.0</summary>
        public const string MonoWasm11_0 = "monowasm11.0";

        /// <summary>Mono WebAssembly AOT with net8.0</summary>
        public const string MonoWasmAot80 = "monowasmaot8.0";
        /// <summary>Mono WebAssembly AOT with net9.0</summary>
        public const string MonoWasmAot90 = "monowasmaot9.0";
        /// <summary>Mono WebAssembly AOT with net10.0</summary>
        public const string MonoWasmAot10_0 = "monowasmaot10.0";
        /// <summary>Mono WebAssembly AOT with net11.0</summary>
        public const string MonoWasmAot11_0 = "monowasmaot11.0";

        // Still experimental
        //public const string CoreWasm11_0 = "corewasm11.0";
    }
}
