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

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters
                .Config
                .GetDiagnosers()
                .SelectMany(diagnoser => diagnoser.Validate(validationParameters));
    }
}