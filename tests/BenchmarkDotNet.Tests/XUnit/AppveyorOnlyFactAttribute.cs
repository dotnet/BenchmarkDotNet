using System;
using BenchmarkDotNet.Portability;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class AppveyorOnlyFactAttribute : FactAttribute
    {
        private static readonly string message = "Test is available only on AppVeyor";

        static AppveyorOnlyFactAttribute()
        {
            string value = Environment.GetEnvironmentVariable("APPVEYOR");
            if (!string.IsNullOrEmpty(value) && value.EqualsWithIgnoreCase("true"))
            {
                message = null;
            }
        }

        public override string Skip => message;
    }
}