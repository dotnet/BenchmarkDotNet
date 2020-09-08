using System;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Portability
{
    public interface IRuntimeInfoWrapper
    {
        Runtime GetCurrentRuntime();
        Platform GetCurrentPlatform();
        Jit GetCurrentJit();
        IntPtr GetCurrentAffinity();
    }
}