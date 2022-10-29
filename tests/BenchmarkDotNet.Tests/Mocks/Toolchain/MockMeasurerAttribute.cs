using System;

namespace BenchmarkDotNet.Tests.Mocks.Toolchain
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MockMeasurerAttribute : Attribute
    {
        public Type MockMeasurerType { get; }

        public MockMeasurerAttribute(Type mockMeasurerType)
        {
            MockMeasurerType = mockMeasurerType;
        }
    }
}