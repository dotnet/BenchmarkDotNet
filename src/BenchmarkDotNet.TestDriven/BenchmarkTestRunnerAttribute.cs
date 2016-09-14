using System;
using TestDriven.Framework;

namespace BenchmarkDotNet.TestDriven
{
    public class BenchmarkTestRunnerAttribute : CustomTestRunnerAttribute
    {
        public BenchmarkTestRunnerAttribute(Type configType) : base(typeof(BenchmarkTestRunner))
        {
            ConfigType = configType;
        }

        public Type ConfigType
        {
            get;
        }
    }
}
