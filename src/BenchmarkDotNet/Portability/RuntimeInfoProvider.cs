using System;
using System.Collections.Generic;
using System.Text;
using Iced.Intel;

namespace BenchmarkDotNet.Portability
{
    public static class RuntimeInfoProvider
    {
        public static IRuntimeInfoWrapper RuntimeInfoWrapper { get; set; } = new RuntimeInfoWrapper();
    }
}
