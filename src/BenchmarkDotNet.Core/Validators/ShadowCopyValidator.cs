using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BenchmarkDotNet.Validators
{
    public class ShadowCopyValidator : IValidator
    {
        public static readonly IValidator Default = new ShadowCopyValidator();

        private ShadowCopyValidator() { }

        public bool TreatsWarningsAsErrors => false;

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            => validationParameters
                .Benchmarks
                .Select(benchmark => benchmark.Target.Type.Assembly)
                .Distinct()
                .Where(assembly => assembly.Location.StartsWith(Path.GetTempPath()))
                .Select(
                    assembly => new ValidationError(
                        false,
                        $"Assembly {assembly} is located in temp. If you are running benchmarks from xUnit you need to disable shadow copy. It's not supported by design."));
    }
}