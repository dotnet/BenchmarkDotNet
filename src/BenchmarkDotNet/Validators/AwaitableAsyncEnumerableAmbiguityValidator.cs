using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using System.Reflection;

namespace BenchmarkDotNet.Validators;

public class AwaitableAsyncEnumerableAmbiguityValidator : IValidator
{
    public static readonly AwaitableAsyncEnumerableAmbiguityValidator DontFailOnError = new AwaitableAsyncEnumerableAmbiguityValidator();

    private AwaitableAsyncEnumerableAmbiguityValidator() { }

    public bool TreatsWarningsAsErrors => false;

    public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters input)
    {
        var validationErrors = new List<ValidationError>();

        foreach (var groupByType in input.Benchmarks.GroupBy(benchmark => benchmark.Descriptor.Type))
        {
            var allMethods = groupByType.Key.GetAllMethods().ToArray();

            CollectErrors<BenchmarkAttribute>(groupByType.Key.Name, allMethods, validationErrors);
            CollectErrors<GlobalSetupAttribute>(groupByType.Key.Name, allMethods, validationErrors);
            CollectErrors<GlobalCleanupAttribute>(groupByType.Key.Name, allMethods, validationErrors);
            CollectErrors<IterationSetupAttribute>(groupByType.Key.Name, allMethods, validationErrors);
            CollectErrors<IterationCleanupAttribute>(groupByType.Key.Name, allMethods, validationErrors);
        }

        return validationErrors.ToAsyncEnumerable();
    }

    private void CollectErrors<T>(string benchmarkClassName, IEnumerable<MethodInfo> allMethods, List<ValidationError> validationErrors) where T : Attribute
    {
        foreach (var method in allMethods)
        {
            if (!method.GetCustomAttributes(false).OfType<T>().Any())
                continue;

            if (!method.ReturnType.IsAsyncEnumerable(out _))
                continue;

            if (!method.ReturnType.IsAwaitable(out _))
                continue;

            validationErrors.Add(new ValidationError(
                TreatsWarningsAsErrors,
                $"[{typeof(T).Name}] method {benchmarkClassName}.{method.Name} returns an awaitable that also matches the async enumerable pattern. It will be only awaited, not enumerated."));
        }
    }
}
