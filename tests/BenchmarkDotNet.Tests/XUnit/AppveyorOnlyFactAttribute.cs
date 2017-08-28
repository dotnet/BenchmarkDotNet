using System;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class AppveyorOnlyFactAttribute : FactAttribute
    {
        private const string message = "Test is available only on the AppVeyor";
        private static readonly string skip;

        static AppveyorOnlyFactAttribute()
        {
            string value = Environment.GetEnvironmentVariable("APPVEYOR");

            if (!string.IsNullOrEmpty(value) && value.EqualsWithIgnoreCase("true"))
                skip = null;
            else
                skip = message;
        }

        public override string Skip => skip;
    }
}