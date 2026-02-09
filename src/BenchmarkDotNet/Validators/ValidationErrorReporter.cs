using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Validators;

[UsedImplicitly]
public static class ValidationErrorReporter
{
    public const string ConsoleErrorPrefix = "// ERROR: ";

    public static async ValueTask<bool> ReportIfAnyAsync(IEnumerable<ValidationError> validationErrors, IHost host)
    {
        bool hasErrors = false;
        foreach (var validationError in validationErrors)
        {
            await host.SendErrorAsync(validationError.Message);
            hasErrors = true;
        }
        return hasErrors;
    }
}