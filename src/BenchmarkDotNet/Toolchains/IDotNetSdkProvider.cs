using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Toolchains
{
    public interface IDotNetSdkProvider
    {
        IEnumerable<string> GetInstalledSdks();
        string CustomDotNetCliPath { get; }
    }
}
