using System;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class AppVeyorOnlyFactAttribute : FactAttribute
    {
        private const string Message = "Test is available only on the AppVeyor";
        private static readonly string skip;

        static AppVeyorOnlyFactAttribute()
        {
            string value = Environment.GetEnvironmentVariable("APPVEYOR");
            bool appVeyorDetected = !string.IsNullOrEmpty(value) && value.EqualsWithIgnoreCase("true");
            skip = appVeyorDetected ? null : Message;
        }

        public override string Skip => skip;
    }
}