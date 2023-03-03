using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Validators
{
    public class ParamsValidator : IValidator
    {
        public static readonly ParamsValidator FailOnError = new ();

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters input) => input.Benchmarks
            .Select(benchmark => benchmark.Descriptor.Type)
            .Distinct()
            .SelectMany(Validate);

        private IEnumerable<ValidationError> Validate(Type type)
        {
            foreach (var memberInfo in type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                var attributes = new Attribute[]
                    {
                        memberInfo.ResolveAttribute<ParamsAttribute>(),
                        memberInfo.ResolveAttribute<ParamsAllValuesAttribute>(),
                        memberInfo.ResolveAttribute<ParamsSourceAttribute>()
                    }
                    .Where(attribute => attribute != null)
                    .ToList();
                if (attributes.IsEmpty())
                    continue;

                string name = $"{type.Name}.{memberInfo.Name}";
                string? attributeString = string.Join(", ", attributes.Select(attribute => $"[{attribute.GetType().Name.Replace(nameof(Attribute), "")}]"));

                if (attributes.Count > 1)
                    yield return new ValidationError(TreatsWarningsAsErrors,
                        $"Unable to use {name} with {attributeString} at the same time. Please, use a single attribute.");

                if (memberInfo is FieldInfo fieldInfo && (fieldInfo.IsLiteral || fieldInfo.IsInitOnly))
                {
                    string modifier = fieldInfo.IsInitOnly ? "readonly" : "constant";
                    yield return new ValidationError(TreatsWarningsAsErrors,
                        $"Unable to use {name} with {attributeString} because it's a {modifier} field. Please, remove the {modifier} modifier.");
                }
            }
        }
    }
}