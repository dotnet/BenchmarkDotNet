using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Validators
{
    public class ReturnValueValidator : ExecutionValidatorBase
    {
        public static ReturnValueValidator DontFailOnError { get; } = new ReturnValueValidator(false);
        public static ReturnValueValidator FailOnError { get; } = new ReturnValueValidator(true);

        private ReturnValueValidator(bool failOnError)
            : base(failOnError) { }

        protected override async IAsyncEnumerable<ValidationError> ValidateAsyncCore(ValidationParameters validationParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var errors = new List<ValidationError>();
            foreach (var typeGroup in validationParameters.Benchmarks.GroupBy(benchmark => benchmark.Descriptor.Type))
            {
                foreach (var parameterGroup in typeGroup.GroupBy(i => i.Parameters, ParameterEqualityComparer.Instance))
                {
                    List<(BenchmarkCase benchmark, object? returnValue)> results = [];
                    int currentErrorCount = errors.Count;

                    foreach (var benchmark in parameterGroup.DistinctBy(i => i.Descriptor.WorkloadMethod))
                    {
                        if (!TryCreateBenchmarkTypeInstance(benchmark.Descriptor.Type, errors, cancellationToken, out var benchmarkTypeInstance))
                        {
                            continue;
                        }
                        if (!TryFillParamsAndGetArgs(benchmark, benchmarkTypeInstance, errors, out var args, cancellationToken))
                        {
                            continue;
                        }
                        if (await TryToCallSetupOrCleanup<GlobalSetupAttribute>(benchmarkTypeInstance, benchmark.Descriptor.GlobalSetupMethod, errors, cancellationToken).ConfigureAwait())
                        {
                            if (await TryToCallSetupOrCleanup<IterationSetupAttribute>(benchmarkTypeInstance, benchmark.Descriptor.IterationSetupMethod, errors, cancellationToken).ConfigureAwait())
                            {
                                var (hasResult, result) = await ExecuteBenchmarkAsync(benchmarkTypeInstance, benchmark, args, errors, cancellationToken).ConfigureAwait();
                                if (hasResult)
                                {
                                    results.Add((benchmark, result));
                                }
                                await TryToCallSetupOrCleanup<IterationCleanupAttribute>(benchmarkTypeInstance, benchmark.Descriptor.IterationCleanupMethod, errors, cancellationToken).ConfigureAwait();
                            }
                            await TryToCallSetupOrCleanup<GlobalCleanupAttribute>(benchmarkTypeInstance, benchmark.Descriptor.GlobalCleanupMethod, errors, cancellationToken).ConfigureAwait();
                        }
                    }

                    if (currentErrorCount < errors.Count || results.Count == 0)
                        continue;

                    if (results.Any(result => !DeepEqualityComparer.Instance.Equals(result.returnValue, results[0].returnValue)))
                    {
                        errors.Add(new ValidationError(
                            TreatsWarningsAsErrors,
                            $"Inconsistent benchmark return values in {typeGroup.Key.GetDisplayName()}: {string.Join(", ", results.Select(r => $"{r.benchmark.Descriptor.WorkloadMethodDisplayInfo}: {r.returnValue}"))} {parameterGroup.Key.DisplayInfo}"));
                    }
                }
            }

            foreach (var error in errors)
            {
                yield return error;
            }
        }

        private async ValueTask<(bool hasResult, object? result)> ExecuteBenchmarkAsync(object benchmarkTypeInstance, BenchmarkCase benchmark, object?[]? args, List<ValidationError> errors, CancellationToken cancellationToken)
        {
            try
            {
                var workloadMethod = benchmark.Descriptor.WorkloadMethod;
                var result = workloadMethod.Invoke(benchmarkTypeInstance, args);
                if (workloadMethod.ReturnType.IsAwaitable(out var awaitableInfo))
                {
                    if (result is null)
                    {
                        errors.Add(new ValidationError(TreatsWarningsAsErrors, $"Awaitable benchmark '{benchmark.DisplayInfo}' returned null", benchmark));
                        return default;
                    }
                    return await DynamicAwaitHelper.AwaitResult(result, awaitableInfo).ConfigureAwait(false);
                }
                else if (workloadMethod.ReturnType.IsAsyncEnumerable(out var asyncEnumerableInfo))
                {
                    if (result is null)
                    {
                        errors.Add(new ValidationError(TreatsWarningsAsErrors, $"Async enumerable benchmark '{benchmark.DisplayInfo}' returned null", benchmark));
                        return default;
                    }
                    return (true, await DynamicAwaitHelper.ToListAsync(result, asyncEnumerableInfo).ConfigureAwait(false));
                }
                else
                {
                    return (workloadMethod.ReturnType != typeof(void), result);
                }
            }
            catch (Exception ex) when (!ExceptionHelper.IsProperCancelation(ex, cancellationToken))
            {
                errors.Add(new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Failed to execute benchmark '{benchmark.DisplayInfo}', exception was: '{GetDisplayExceptionMessage(ex)}'",
                    benchmark));
                return default;
            }
        }
    }
}