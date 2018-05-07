using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Validators
{
    public class BenchmarkClassValidator: IValidator
    {
        public static readonly IValidator Default = new BenchmarkClassValidator();
        
        public bool TreatsWarningsAsErrors => false;

        private BenchmarkClassValidator() { }
        
        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var notValid = validationParameters.Benchmarks.Where(x => x.Target.Type.IsGenericType)
                                                          .Where(HasPrivateGenericArguments).ToList();
            foreach (var benchmark in notValid)
            {
                yield return new ValidationError(true, $"Generic class {benchmark.Target.Type.GetDisplayName()} has non public generic argument(s)");
            }
        }

        private bool HasPrivateGenericArguments(Benchmark benchmark) => benchmark.Target.Type.GetGenericArguments().Any(a => !a.IsPublic
                                                                                                                          && !a.IsNestedPublic);
        
    }
}