using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Validators
{
    class CustomEnvironmentInfoValidator : IValidator
    {
        public static readonly CustomEnvironmentInfoValidator FailOnError = new CustomEnvironmentInfoValidator();

        private CustomEnvironmentInfoValidator() { }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters input)
        {
            var validationErrors = new List<ValidationError>();

            foreach (var groupByType in input.Benchmarks.GroupBy(benchmark => benchmark.Descriptor.Type))
            {
                var customEnvInfoMethods =
                    groupByType.Key
                        .GetAllMethods()
                        .Where(m => m.GetCustomAttributes().OfType<CustomEnvironmentInfoAttribute>().Any());

                validationErrors.AddRange(customEnvInfoMethods.SelectMany(ValidateAttibute));
            }

            return validationErrors;
        }

        private IEnumerable<ValidationError> ValidateAttibute(MethodInfo methodInfo)
        {
            if (!methodInfo.IsPublic)
                yield return new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Custom environment info method {methodInfo.Name} has incorrect access modifiers.\nMethod must be public.");

            if (!methodInfo.IsStatic)
                yield return new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Custom environment info method {methodInfo.Name} is non-static.\nMethod must be static.");

            if (methodInfo.GetParameters().Any())
                yield return new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Custom environment info method {methodInfo.Name} has incorrect signature.\nMethod shouldn't have any arguments.");

            var returnType = methodInfo.ReturnType;
            if (returnType != typeof(string) && (!typeof(IEnumerable<string>).IsAssignableFrom(returnType)))
            {
                yield return new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Custom environment info method {methodInfo.Name} has incorrect signature.\nMethod should return string or IEnumerable<string>.");
            }
        }
    }
}
