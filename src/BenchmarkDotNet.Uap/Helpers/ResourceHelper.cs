using BenchmarkDotNet.Helpers;
using System.Reflection;

namespace BenchmarkDotNet.Uap.Helpers
{
    internal class ResourceHelperLocal
    {
        public static ResourceHelper UapHelper = new ResourceHelper("BenchmarkDotNet.Uap.Templates.", typeof(ResourceHelperLocal).GetTypeInfo().Assembly);
    }
}