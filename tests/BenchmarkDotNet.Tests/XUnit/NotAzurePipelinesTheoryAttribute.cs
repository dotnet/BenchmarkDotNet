using System;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit {
    public class NotAzurePipelinesTheoryAttribute : TheoryAttribute
    {
        private const string Message = "Test doesn't work on Azure Pipelines.";
        
        // ReSharper disable once VirtualMemberCallInConstructor
        public NotAzurePipelinesTheoryAttribute()
        {
            string value = Environment.GetEnvironmentVariable("TF_BUILD"); // https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables?view=vsts#system-variables
            Skip = !string.IsNullOrEmpty(value) ? Message : null;
        }
    }
}