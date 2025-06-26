using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Environments;
using static System.Runtime.InteropServices.RuntimeInformation;

namespace BenchmarkDotNet.Portability
{
    internal static partial class RuntimeInformation
    {
        public static Platform GetCurrentPlatform()
        {
            // these are not part of .NET Standard 2.0, so we use hardcoded values taken from
            // https://github.com/dotnet/runtime/blob/080fcae7eaa8367abf7900e08ff2e52e3efea5bf/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/Architecture.cs#L9
            const Architecture Wasm = (Architecture)4;
            const Architecture S390x = (Architecture)5;
            const Architecture LoongArch64 = (Architecture)6;
            const Architecture Armv6 = (Architecture)7;
            const Architecture Ppc64le = (Architecture)8;
            const Architecture RiscV64 = (Architecture)9;

            switch (ProcessArchitecture)
            {
                case Architecture.Arm:
                    return Platform.Arm;
                case Architecture.Arm64:
                    return Platform.Arm64;
                case Architecture.X64:
                    return Platform.X64;
                case Architecture.X86:
                    return Platform.X86;
                case Wasm:
                    return Platform.Wasm;
                case S390x:
                    return Platform.S390x;
                case LoongArch64:
                    return Platform.LoongArch64;
                case Armv6:
                    return Platform.Armv6;
                case Ppc64le:
                    return Platform.Ppc64le;
                case RiscV64:
                    return Platform.RiscV64;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}