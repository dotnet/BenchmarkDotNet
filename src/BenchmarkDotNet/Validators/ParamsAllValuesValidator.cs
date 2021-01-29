using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Validators
{
    public class ParamsAllValuesValidator : IValidator
    {
        public static readonly ParamsAllValuesValidator FailOnError = new ParamsAllValuesValidator();

        public bool TreatsWarningsAsErrors => true;

        private ParamsAllValuesValidator() { }

        private const BindingFlags ReflectionFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public IEnumerable<ValidationError> Validate(ValidationParameters input) =>
            input.Benchmarks
                .Select(benchmark => benchmark.Descriptor.Type)
                .Distinct()
                .SelectMany(type => type.GetTypeMembersWithGivenAttribute<ParamsAllValuesAttribute>(ReflectionFlags))
                .Distinct()
                .Select(member => GetErrorOrDefault(member.ParameterType))
                .Where(error => error != null);

        private bool IsBool(Type paramType) => paramType == typeof(bool);
        private bool IsEnum(Type paramType) => paramType.GetTypeInfo().IsEnum;
        private bool IsFlagsEnum(Type paramType)
        {
            var typeInfo = paramType.GetTypeInfo();
            return typeInfo.IsEnum && typeInfo.IsDefined(typeof(FlagsAttribute));
        }
        private bool IsNullable(Type paramType, out Type underlyingType)
        {
            underlyingType = Nullable.GetUnderlyingType(paramType);
            return underlyingType != null;
        }

        private ValidationError GetErrorOrDefault(Type parameterType)
        {
            switch (parameterType)
            {
                case Type t when IsNullable(t, out Type underType):
                    return GetErrorOrDefault(underType);

                case Type t when IsFlagsEnum(t):
                    return new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Unable to use {parameterType.Name} with [ParamsAllValues], because it's flags enum.");

                case Type t when IsBool(t) || IsEnum(t):
                    return default;

                default:
                    return new ValidationError(
                        TreatsWarningsAsErrors,
                        $"Type {parameterType.Name} cannot be used with [ParamsAllValues], allowed types are: bool, enum types and nullable type for another allowed type.");
            }
        }
    }
}
