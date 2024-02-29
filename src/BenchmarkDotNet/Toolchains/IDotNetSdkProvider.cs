using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Toolchains
{
    public interface IDotNetSdkProvider
    {
        string CustomDotNetCliPath { get; }
        IEnumerable<string> GetInstalledDotNetSdks();
    }
}
