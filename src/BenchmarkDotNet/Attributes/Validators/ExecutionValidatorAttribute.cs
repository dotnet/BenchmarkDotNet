using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes
{
    public class ExecutionValidatorAttribute : ValidatorConfigBaseAttribute
    {
        public ExecutionValidatorAttribute()
            : this(true) { }

        public ExecutionValidatorAttribute(bool failOnError)
            : base(failOnError ? ExecutionValidator.FailOnError : ExecutionValidator.DontFailOnError) { }
    }
}