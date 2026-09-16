using BenchmarkDotNet.Helpers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Validators
{
    internal class CompositeValidator : IValidator
    {
        private readonly ImmutableHashSet<IValidator> validators;

        public CompositeValidator(ImmutableHashSet<IValidator> validators) => this.validators = validators;

        /// <summary>
        /// returns true if any of the validators has TreatsWarningsAsErrors == true
        /// </summary>
        public bool TreatsWarningsAsErrors
            => validators.Any(validator => validator.TreatsWarningsAsErrors);

        // Written out rather than composed with async LINQ: a validator that awaits user code suspends, and an
        // operator driving it would resume on whatever SynchronizationContext is ambient around the run. BenchmarkDotNet
        // installs none of its own, so that is the caller's - which may be single-threaded and, while the pump blocks
        // its thread, unable to run the continuation at all.
        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => ValidateAsyncCore(validationParameters);

        // The token reaches this through the consumer's ConfigureAwait/WithCancellation on the returned enumerable,
        // and is forwarded so it reaches each validator's own enumerator (the ExecutionValidatorBase pattern).
        private async IAsyncEnumerable<ValidationError> ValidateAsyncCore(ValidationParameters validationParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var reported = new HashSet<ValidationError>();

            foreach (var validator in validators)
            {
#pragma warning disable CA2007
                await foreach (var error in validator.ValidateAsync(validationParameters).ConfigureAwait(cancellationToken))
#pragma warning restore CA2007
                {
                    if (reported.Add(error))
                    {
                        yield return error;
                    }
                }
            }
        }
    }
}