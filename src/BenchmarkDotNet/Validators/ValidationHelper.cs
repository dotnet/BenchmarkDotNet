using System.Collections.Generic;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Validators
{
    public static class ValidationHelper
    {
        public const string ConsoleErrorPrefix = "// ERROR: ";
        
        public static bool CheckForErrors(IEnumerable<ValidationError> validationErrors, IHost host)
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