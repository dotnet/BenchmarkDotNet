using System;
using System.Collections.Generic;
using System.Text;
using Iced.Intel;

namespace BenchmarkDotNet.Portability
{
    public static class RuntimeInformationWrapperProvider
    {
        public static IRuntimeInformationWrapper RuntimeInformationWrapper { get; set; } = new RuntimeInformationWrapper();
    }
}
