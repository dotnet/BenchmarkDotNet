using BenchmarkDotNet.Validators;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class ReturnValueValidatorAttribute : ValidatorConfigBaseAttribute
    {
        public ReturnValueValidatorAttribute()
            : this(true) { }

        public ReturnValueValidatorAttribute(bool failOnError)
            : base(failOnError ? ReturnValueValidator.FailOnError : ReturnValueValidator.DontFailOnError) { }
    }
}