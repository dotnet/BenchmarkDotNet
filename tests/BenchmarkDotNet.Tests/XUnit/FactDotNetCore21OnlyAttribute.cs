using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class FactDotNetCore21OnlyAttribute : FactAttribute
    {
        public FactDotNetCore21OnlyAttribute(string skipReason)
        {
#if !NETCOREAPP2_1
            Skip = skipReason;
#endif
        }
    }
}