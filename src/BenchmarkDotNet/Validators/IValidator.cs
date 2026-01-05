using System.Collections.Generic;

namespace BenchmarkDotNet.Validators
{
    public interface IValidator
    {
        bool TreatsWarningsAsErrors { get; }

        IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters);
    }
}