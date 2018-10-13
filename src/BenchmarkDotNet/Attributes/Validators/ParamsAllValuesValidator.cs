using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Validators
{
    class ParamsAllValuesValidator : IValidator
    {
        public static readonly ParamsAllValuesValidator FailOnError = new ParamsAllValuesValidator();

        private ParamsAllValuesValidator() { }

        public bool TreatsWarningsAsErrors => true;

        public IEnumerable<ValidationError> Validate(ValidationParameters input)
        {
            var validationErrors = new List<ValidationError>();

            foreach (var groupByType in input.Benchmarks.GroupBy(benchmark => benchmark.Descriptor.Type))
            {
                const BindingFlags reflectionFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var allMembers = groupByType.Key.GetTypeMembersWithGivenAttribute<ParamsAllValuesAttribute>(reflectionFlags);
                validationErrors.AddRange(allMembers.Select(member => member.ParameterType).SelectMany(ValidateAttibute));
            }

            return validationErrors;
        }

        private IEnumerable<ValidationError> ValidateAttibute(Type parameterType)
        {
            var nullableUnderlyingType = Nullable.GetUnderlyingType(parameterType);
            var typeInfo = parameterType.GetTypeInfo();

            if (typeInfo.IsEnum && typeInfo.IsDefined(typeof(FlagsAttribute)))
            {
                yield return new ValidationError(
                    TreatsWarningsAsErrors, 
                    $"Unable to use {parameterType.Name} with [ParamsAllValues], because it's flags enum.");
            }
            else if (nullableUnderlyingType != null)
            {
                foreach (var error in ValidateAttibute(nullableUnderlyingType))
                {
                    yield return error;
                }
            }
            else if (parameterType != typeof(bool) && !typeInfo.IsEnum && nullableUnderlyingType == null)
            {
                yield return new ValidationError(
                    TreatsWarningsAsErrors,
                    $"Type {parameterType.Name} cannot be used with [ParamsAllValues], allowed types are: bool, enum types and nullable type for another allowed type.");
            }
        }
    }
}
