using BenchmarkDotNet.Helpers;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Validators
{
    public class DiagnosersValidator : IValidator
    {
        public static readonly IValidator Composite = new DiagnosersValidator();

        private DiagnosersValidator()
        {
        }

        public bool TreatsWarningsAsErrors => true;

        // Written out rather than composed with async LINQ - see CompositeValidator.ValidateAsync for why. The
        // diagnosers here are third-party implementations, so their sequences can suspend on anything.
        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => ValidateAsyncCore(validationParameters);

        private static async IAsyncEnumerable<ValidationError> ValidateAsyncCore(ValidationParameters validationParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var diagnoser in validationParameters.Config.GetDiagnosers())
            {
#pragma warning disable CA2007
                await foreach (var error in diagnoser.ValidateAsync(validationParameters).ConfigureAwait(cancellationToken))
#pragma warning restore CA2007
                {
                    yield return error;
                }
            }
        }
    }
}