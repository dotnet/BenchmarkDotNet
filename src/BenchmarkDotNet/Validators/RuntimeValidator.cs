using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;

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
        var nullRuntimeBenchmarks = allBenchmarks.Where(x => x.Job.Environment.Runtime == null).ToArray();

        // There is no validation error if all the runtimes are set or if all the runtimes are null.
        if (allBenchmarks.Length == nullRuntimeBenchmarks.Length)
        {
            return [];
        }

        var errors = new List<ValidationError>();
        foreach (var benchmark in nullRuntimeBenchmarks)
        {
            var job = benchmark.Job;
            var jobText = job.HasValue(CharacteristicObject.IdCharacteristic)
                ? job.Id
                : CharacteristicSetPresenter.Display.ToPresentation(job); // Use job text representation instead for auto generated JobId.

            var message = $"Job({jobText}) doesn't have a Runtime characteristic. It's recommended to specify runtime by using WithRuntime explicitly.";
            errors.Add(new ValidationError(false, message));
        }
        return errors;
    }
}
