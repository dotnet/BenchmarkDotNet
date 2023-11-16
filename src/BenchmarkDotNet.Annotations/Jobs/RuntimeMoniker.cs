using System;

namespace BenchmarkDotNet.Jobs
{
    public enum RuntimeMoniker
    {
        /// <summary>
        /// the same Runtime as the host Process (default setting)
        /// </summary>
        HostProcess = 0,

        /// <summary>
        /// not recognized, possibly a new version of .NET Core
        /// </summary>
        NotRecognized,

        /// <summary>
        /// Mono
        /// </summary>
        Mono,

        /// <summary>
        /// .NET 4.6.1
        /// </summary>
        Net461,

        /// <summary>
        /// .NET 4.6.2
        /// </summary>
        Net462,

        /// <summary>
        /// .NET 4.7
        /// </summary>
        Net47,

        /// <summary>
        /// .NET 4.7.1
        /// </summary>
        Net471,

        /// <summary>
        /// .NET 4.7.2
        /// </summary>
        Net472,

        /// <summary>
        /// .NET 4.8
        /// </summary>
        Net48,

        /// <summary>
        /// .NET 4.8.1
        /// </summary>
        Net481,

        /// <summary>
        /// .NET Core 2.0
        /// </summary>
        NetCoreApp20,

        /// <summary>
        /// .NET Core 2.1
        /// </summary>
        NetCoreApp21,

        /// <summary>
        /// .NET Core 2.2
        /// </summary>
        NetCoreApp22,

        /// <summary>
        /// .NET Core 3.0
        /// </summary>
        NetCoreApp30,

        /// <summary>
        /// .NET Core 3.1
        /// </summary>
        NetCoreApp31,

        /// <summary>
        /// .NET Core 5.0 aka ".NET 5"
        /// </summary>
        [Obsolete("Please switch to the 'RuntimeMoniker.Net50'")]
        NetCoreApp50,

        /// <summary>
        /// .NET 5.0
        /// </summary>
        Net50, // it's after NetCoreApp50 in the enum definition because the value of enumeration is used for framework version comparison using > < operators

        /// <summary>
        /// .NET 6.0
        /// </summary>
        Net60,

        /// <summary>
        /// .NET 7.0
        /// </summary>
        Net70,

        /// <summary>
        /// .NET 8.0
        /// </summary>
        Net80,

        /// <summary>
        /// .NET 9.0
        /// </summary>
        Net90,

        /// <summary>
        /// NativeAOT compiled as net6.0
        /// </summary>
        NativeAot60,

        /// <summary>
        /// NativeAOT compiled as net7.0
        /// </summary>
        NativeAot70,

        /// <summary>
        /// NativeAOT compiled as net8.0
        /// </summary>
        NativeAot80,

        /// <summary>
        /// NativeAOT compiled as net9.0
        /// </summary>
        NativeAot90,

        /// <summary>
        /// WebAssembly with default .Net version
        /// </summary>
        Wasm,

        /// <summary>
        /// WebAssembly with net5.0
        /// </summary>
        WasmNet50,

        /// <summary>
        /// WebAssembly with net6.0
        /// </summary>
        WasmNet60,

        /// <summary>
        /// WebAssembly with net7.0
        /// </summary>
        WasmNet70,

        /// <summary>
        /// WebAssembly with net8.0
        /// </summary>
        WasmNet80,

        /// <summary>
        /// WebAssembly with net9.0
        /// </summary>
        WasmNet90,

        /// <summary>
        /// Mono with the Ahead of Time LLVM Compiler backend
        /// </summary>
        MonoAOTLLVM,

        /// <summary>
        /// Mono with the Ahead of Time LLVM Compiler backend and net6.0
        /// </summary>
        MonoAOTLLVMNet60,

        /// <summary>
        /// Mono with the Ahead of Time LLVM Compiler backend and net7.0
        /// </summary>
        MonoAOTLLVMNet70,

        /// <summary>
        /// Mono with the Ahead of Time LLVM Compiler backend and net8.0
        /// </summary>
        MonoAOTLLVMNet80,

        /// <summary>
        /// Mono with the Ahead of Time LLVM Compiler backend and net9.0
        /// </summary>
        MonoAOTLLVMNet90,

        /// <summary>
        /// .NET 6 using MonoVM (not CLR which is the default)
        /// </summary>
        Mono60,

        /// <summary>
        /// .NET 7 using MonoVM (not CLR which is the default)
        /// </summary>
        Mono70,

        /// <summary>
        /// .NET 8 using MonoVM (not CLR which is the default)
        /// </summary>
        Mono80,

        /// <summary>
        /// .NET 9 using MonoVM (not CLR which is the default)
        /// </summary>
        Mono90,
    }
}
