using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Validators
{
    public class DeferredExecutionValidator : IValidator
    {
        public static readonly IValidator DontFailOnError = new DeferredExecutionValidator(false);
        public static readonly IValidator FailOnError = new DeferredExecutionValidator(true);

        private DeferredExecutionValidator(bool failOnError) => TreatsWarningsAsErrors = failOnError;

        public bool TreatsWarningsAsErrors { get; }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters.Benchmarks
                .Where(benchmark => IsDeferredExecution(benchmark.Descriptor.WorkloadMethod.ReturnType))
                .Select(benchmark =>
                    new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Benchmark {benchmark.Descriptor.Type.Name}.{benchmark.Descriptor.WorkloadMethod.Name} returns a deferred execution result ({benchmark.Descriptor.WorkloadMethod.ReturnType.GetCorrectCSharpTypeName(false, false)}). " +
                        "You need to either change the method declaration to return a materialized result or consume it on your own. You can use .Consume() extension method to do that.",
                        benchmark));

        private bool IsDeferredExecution(Type returnType)
        {
            if (returnType.IsByRef && !returnType.IsGenericType)
                return IsDeferredExecution(returnType.GetElementType());

            if (returnType.IsGenericType && (returnType.GetGenericTypeDefinition() == typeof(Task<>) || returnType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
                return IsDeferredExecution(returnType.GetGenericArguments().Single());

            if (returnType == typeof(IEnumerable) || returnType == typeof(IQueryable))
                return true;

            if (!returnType.IsGenericType)
                return false;

            return returnType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                || returnType.GetGenericTypeDefinition() == typeof(IQueryable<>)
                || returnType.GetGenericTypeDefinition() == typeof(Lazy<>);
        }
    }
}