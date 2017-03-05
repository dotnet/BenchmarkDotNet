using BenchmarkDotNet.Helpers;
using System;
using System.IO;
using System.Reflection;

namespace BenchmarkDotNet.Toolchains.Uap.Helpers
{
    internal partial class ResourceHelperLocal
    {
        public static ResourceHelper UapHelper = new ResourceHelper("BenchmarkDotNet.Toolchains.Uap.Templates.", "BenchmarkDotNet.Toolchains.Uap.Templates.", typeof(ResourceHelperLocal).GetTypeInfo().Assembly);
    }
}