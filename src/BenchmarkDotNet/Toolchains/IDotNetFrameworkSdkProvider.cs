using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Toolchains
{
    public interface IDotNetFrameworkSdkProvider
    {
        IEnumerable<string> GetInstalledFrameworkSdks();
    }
}