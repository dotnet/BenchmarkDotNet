using BenchmarkDotNet.Extensions;
using System.Collections;

namespace BenchmarkDotNet.Validators
{
    public class DeferredExecutionValidator : IValidator
    {
        public static readonly IValidator DontFailOnError = new DeferredExecutionValidator(false);
        public static readonly IValidator FailOnError = new DeferredExecutionValidator(true);

        private DeferredExecutionValidator(bool failOnError) => TreatsWarningsAsErrors = failOnError;

        public bool TreatsWarningsAsErrors { get; }

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => validationParameters.Benchmarks
                .Where(benchmark => IsDeferredExecution(benchmark.Descriptor.WorkloadMethod.ReturnType))
                .Select(benchmark =>
                    new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Benchmark {benchmark.Descriptor.Type.Name}.{benchmark.Descriptor.WorkloadMethod.Name} returns a deferred execution result ({benchmark.Descriptor.WorkloadMethod.ReturnType.GetCorrectCSharpTypeName(false, false)}). " +
                        "You need to either change the method declaration to return a materialized result or consume it on your own. You can use .Consume() extension method to do that.",
                        benchmark)
                )
                .ToAsyncEnumerable();

        private static bool IsDeferredExecution(Type returnType)
        {
            if (returnType.IsByRef && !returnType.IsGenericType)
                return IsDeferredExecution(returnType.GetElementType()!);

            if (returnType.IsAwaitable(out var awaitableInfo))
                return IsNestedDeferredExecution(awaitableInfo.ResultType);

            if (returnType.IsAsyncEnumerable(out var asyncEnumerableInfo))
                return IsNestedDeferredExecution(asyncEnumerableInfo.ItemType);

            return IsDeferredExecutionCore(returnType);

            // We support consuming returned awaitables and async enumerables, but we don't support nested consumption, e.g. Task<Task> or IAsyncEnumerable<IAsyncEnumerable<int>>.
            static bool IsNestedDeferredExecution(Type returnType)
            {
                return IsDeferredExecutionCore(returnType)
                    || returnType.IsAwaitable(out _)
                    || returnType.IsAsyncEnumerable(out _);
            }

            static bool IsDeferredExecutionCore(Type returnType)
            {
                if (returnType == typeof(IEnumerable) || returnType == typeof(IEnumerator) || returnType == typeof(IQueryable))
                    return true;

                if (!returnType.IsGenericType)
                    return false;

                var genericTypeDefinition = returnType.GetGenericTypeDefinition();

                return genericTypeDefinition == typeof(IEnumerable<>)
                    || genericTypeDefinition == typeof(IEnumerator<>)
                    || genericTypeDefinition == typeof(IQueryable<>)
                    || genericTypeDefinition == typeof(Lazy<>);
            }
        }
    }
}