using System.Collections.Generic;

namespace BenchmarkDotNet.Validators
{
    public interface IValidator
    {
        bool TreatsWarningsAsErrors { get; }

        IEnumerable<ValidationError> Validate(ValidationParameters validationParameters);
    }
}