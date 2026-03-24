using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Validators
{
    public class BenchmarkCancellationValidator : IValidator
    {
        public static readonly BenchmarkCancellationValidator FailOnError = new();

        public bool TreatsWarningsAsErrors => true;

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters input) => input.Benchmarks
            .Select(benchmark => benchmark.Descriptor.Type)
            .Distinct()
            .ToAsyncEnumerable()
            .SelectMany(ValidateAsync);

        private async IAsyncEnumerable<ValidationError> ValidateAsync(Type type)
        {
            const BindingFlags reflectionFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance |
                                                 BindingFlags.FlattenHierarchy;
            foreach (var memberInfo in type.GetMembers(reflectionFlags))
            {
                var attribute = memberInfo.ResolveAttribute<BenchmarkCancellationAttribute>();
                if (attribute == null)
                    continue;

                string name = $"{type.Name}.{memberInfo.Name}";
                string attributeString = "[BenchmarkCancellation]";

                if (memberInfo is FieldInfo fieldInfo)
                {
                    if (fieldInfo.FieldType != typeof(CancellationToken))
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because its type is {fieldInfo.FieldType.Name}. Only CancellationToken is supported.");

                    if (fieldInfo.IsInitOnly)
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because it's a readonly field. Please, remove the readonly modifier.");

                    if (!fieldInfo.IsPublic)
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because it's not public. Please, make it public.");
                }

                if (memberInfo is PropertyInfo propertyInfo)
                {
                    if (propertyInfo.PropertyType != typeof(CancellationToken))
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because its type is {propertyInfo.PropertyType.Name}. Only CancellationToken is supported.");

                    if (propertyInfo.SetMethod == null)
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because it has no setter. Please, provide a setter.");

                    if (propertyInfo.SetMethod != null && !propertyInfo.SetMethod.IsPublic)
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because its setter is not public. Please, make the setter public.");
                }
            }
        }
    }
}
