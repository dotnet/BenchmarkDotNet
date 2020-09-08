using System;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.Portability
{
    public interface IRuntimeInformationWrapper
    {
        Runtime GetCurrentRuntime();
        Platform GetCurrentPlatform();
        Jit GetCurrentJit();
        IntPtr GetCurrentAffinity();
    }
}