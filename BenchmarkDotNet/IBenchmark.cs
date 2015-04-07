using System;
using System.Collections.Generic;

namespace BenchmarkDotNet
{
    public interface IBenchmark
    {
        string Name { get; }
        Action Initialize { get; }
        Action Action { get; }
        Action Clean { get; }
        Dictionary<string, object> Settings { get; }
    }
}