using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Validators
{
    public class DiagnosersValidator : IValidator
    {
        public static readonly IValidator Composite = new DiagnosersValidator();

        private DiagnosersValidator()
        {
        }

        public bool TreatsWarningsAsErrors => true;

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters)
            => validationParameters
                .Config
                .GetDiagnosers()
                .ToAsyncEnumerable()
                .SelectMany(diagnoser => diagnoser.ValidateAsync(validationParameters));
    }
}