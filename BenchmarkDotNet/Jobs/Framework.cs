using System;

namespace BenchmarkDotNet.Jobs
{
    // TODO: Drop V35 in next version
    public enum Framework
    {
        Host,
        [Obsolete("BenchmarkDotNet does not support .NET 3.5", true)]
        V35,
        V40,
        V45,
        V451,
        V452,
        V46,
        V461,
        V462
    }
}