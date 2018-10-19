using Xunit;

namespace BenchmarkDotNet.Tests.XUnit 
{
    public class WorkInProgressTheoryAttribute : TheoryAttribute
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public WorkInProgressTheoryAttribute()
        {
            Skip = "Work in progress. Skip this test...";
        }
    }
}