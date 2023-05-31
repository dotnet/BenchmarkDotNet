using System;

namespace BenchmarkDotNet.IntegrationTests.Xunit
{
    public class MisconfiguredEnvironmentException : Exception
    {
        public MisconfiguredEnvironmentException(string message) : base(message) { }

        public string SkipMessage => $"Skip this test because the environment is misconfigured ({Message})";
    }
}