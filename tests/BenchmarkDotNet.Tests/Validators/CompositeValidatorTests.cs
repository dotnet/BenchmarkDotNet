using System;
using System.Linq;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.Tests.Validators
{
    public class CompositeValidatorTests
    {
        [Fact]
        public void ThreatWarningsAsErrorsOverridesDefaultBehaviour()
        {
            var compositeValidator = new CompositeValidator(
                new IValidator[]
                {
                    ExecutionValidator.DontFailOnError,
                    ExecutionValidator.FailOnError
                });

            Assert.True(compositeValidator.TreatsWarningsAsErrors);
            Assert.Contains(ExecutionValidator.FailOnError, compositeValidator.Validators);
            Assert.DoesNotContain(ExecutionValidator.DontFailOnError, compositeValidator.Validators);
        }

        [Fact]
        public void BaseLineValidatorIsMandatory()
        {
            var compositeValidator = new CompositeValidator(Array.Empty<IValidator>());

            Assert.Contains(BaselineValidator.FailOnError, compositeValidator.Validators);
        }

        [Fact]
        public void DuplicatesAreEliminated()
        {
            var compositeValidator = new CompositeValidator(Enumerable.Repeat(JitOptimizationsValidator.DontFailOnError, 3).ToArray());

            Assert.Single(
                compositeValidator.Validators,
                validator => ReferenceEquals(validator, JitOptimizationsValidator.DontFailOnError));
        }
    }
}