using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Validators;

/// <summary>
/// Validator for runtime characteristic.
/// <see href="https://github.com/dotnet/BenchmarkDotNet/issues/2609" />
/// </summary>
public class RuntimeValidator : IValidator
{
    public static readonly IValidator DontFailOnError = new RuntimeValidator();

    private RuntimeValidator() { }

    public bool TreatsWarningsAsErrors => false;

    public IEnumerable<ValidationError> Validate(ValidationParameters input)
    {
        var allBenchmarks = input.Benchmarks.ToArray();

        var runtimes = allBenchmarks.Select(x => x.Job.Environment.Runtime)
                                    .Distinct()
                                    .ToArray();

        if (runtimes.Length > 1 && runtimes.Contains(null))
        {
            // GetRuntime() method returns current environment's runtime if RuntimeCharacteristic is not set.
            var message = "There are benchmarks that job don't have a Runtime characteristic. It's recommended explicitly specify runtime by `WithRuntime`.";
            yield return new ValidationError(false, message);
        }
    }
}
