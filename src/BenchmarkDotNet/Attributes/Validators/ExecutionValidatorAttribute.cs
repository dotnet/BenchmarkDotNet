using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class ExecutionValidatorAttribute : ValidatorConfigBaseAttribute
    {
        public ExecutionValidatorAttribute()
            : this(true) { }

        public ExecutionValidatorAttribute(bool failOnError)
            : base(failOnError ? ExecutionValidator.FailOnError : ExecutionValidator.DontFailOnError) { }
    }
}