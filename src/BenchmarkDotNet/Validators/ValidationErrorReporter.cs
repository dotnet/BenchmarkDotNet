using System.Collections.Generic;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Validators
{
    public static class ValidationErrorReporter
    {
        public const string ConsoleErrorPrefix = "// ERROR: ";

        [UsedImplicitly] // Generated benchmarks
        public static bool ReportIfAny(IEnumerable<ValidationError> validationErrors, IHost host)
        {
            bool hasErrors = false;
            foreach (var validationError in validationErrors)
            {
                host.SendError(validationError.Message);
                hasErrors = true;
            }
            return hasErrors;
        }
    }
}