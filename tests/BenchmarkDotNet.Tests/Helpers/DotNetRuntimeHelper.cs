using BenchmarkDotNet.Portability;
using System;

namespace BenchmarkDotNet
{
    public static class DotNetRuntimeHelper
    {
        public static string GetExpectedDotNetCoreRuntimeName()
        {
            return Environment.GetEnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX") == "2"
                ? ".NET 10.0"
                : ".NET 8.0";
        }
    }
}
