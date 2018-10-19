using Xunit;

namespace BenchmarkDotNet.Tests.XUnit 
{
    public class WorkInProgressFactAttribute : FactAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public WorkInProgressFactAttribute()
        {
            Skip = "Work in progress. Skip this test...";
        }
    }
}