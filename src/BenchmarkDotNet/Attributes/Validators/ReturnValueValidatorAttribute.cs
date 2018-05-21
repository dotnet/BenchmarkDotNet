using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes.Validators
{
    public class ReturnValueValidatorAttribute : ValidatorConfigBaseAttribute
    {
        public ReturnValueValidatorAttribute()
            : this(true) { }

        public ReturnValueValidatorAttribute(bool failOnError)
            : base(failOnError ? ReturnValueValidator.FailOnError : ReturnValueValidator.DontFailOnError) { }
    }
}