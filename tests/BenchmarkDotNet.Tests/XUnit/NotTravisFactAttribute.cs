using System;
using Xunit;

namespace BenchmarkDotNet.Tests.XUnit
{
    public class NotTravisFactAttributeAttribute : FactAttribute
    {
        private const string Message = "Test is not available on Travis";
        private static readonly string skip;

        static NotTravisFactAttributeAttribute()
        {
            string value = Environment.GetEnvironmentVariable("TRAVIS"); // https://docs.travis-ci.com/user/environment-variables/#Default-Environment-Variables
            skip = !string.IsNullOrEmpty(value) ? Message : null;
        }

        public override string Skip => skip;
    }
}