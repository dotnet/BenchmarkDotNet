using System;

using BenchmarkDotNet.Validators;

using Xunit;

namespace BenchmarkDotNet.Tests.Validators
{
    public class CompositeValidatorTests
    {
        [Fact]
        public void ValidatorsAreNotAltered()
        {
            var validators = new IValidator[]
            {
                ExecutionValidator.DontFailOnError,
                ExecutionValidator.FailOnError
            };
            var compositeValidator = new CompositeValidator(validators);

            Assert.Equal(validators, compositeValidator.Validators);
        }

        [Fact]
        public void NoMandatoryValidatorsAdded()
        {
            var compositeValidator = new CompositeValidator(Array.Empty<IValidator>());

            Assert.Empty(compositeValidator.Validators);
        }
    }
}