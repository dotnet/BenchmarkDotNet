using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using System.Reflection;

namespace BenchmarkDotNet.Validators;

/// <summary>
/// Validates that every [ParamsSource]/[ArgumentsSource] member is one BenchmarkDotNet can reach and read values
/// from. The runtime counterpart of the BDN1306, BDN1308, BDN1311 and BDN1504 analyzer rules.
/// </summary>
public class SourceReturnTypeValidator : IValidator
{
    public static readonly SourceReturnTypeValidator FailOnError = new();

    public bool TreatsWarningsAsErrors => true;

    public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters input)
    {
        var fromParams = input.Benchmarks
            .Select(benchmark => benchmark.Descriptor.Type)
            .Distinct()
            .SelectMany(ValidateParamsSources);

        var fromArguments = input.Benchmarks
            .Select(benchmark => (benchmark.Descriptor.Type, benchmark.Descriptor.WorkloadMethod))
            .Distinct()
            .SelectMany(descriptor => ValidateArgumentsSource(descriptor.Type, descriptor.WorkloadMethod));

        return fromParams.Concat(fromArguments).ToAsyncEnumerable();
    }

    private IEnumerable<ValidationError> ValidateParamsSources(Type type)
    {
        foreach (var member in type.GetTypeMembersWithGivenAttribute<ParamsSourceAttribute>(ReflectionExtensions.ParameterMemberFlags))
        {
            if (Validate(member.Attribute.Type ?? type, member.Attribute.Name, nameof(ParamsSourceAttribute), $"{type.Name}.{member.Name}") is { } error)
            {
                yield return error;
            }
        }
    }

    private IEnumerable<ValidationError> ValidateArgumentsSource(Type type, MethodInfo benchmark)
    {
        if (benchmark.ResolveAttribute<ArgumentsSourceAttribute>() is { } attribute
            && Validate(attribute.Type ?? type, attribute.Name, nameof(ArgumentsSourceAttribute), $"{type.Name}.{benchmark.Name}") is { } error)
        {
            yield return error;
        }
    }

    private ValidationError? Validate(Type sourceType, string sourceName, string attributeName, string owner)
    {
        var source = sourceType.FindSourceMember(sourceName);
        if (source == null)
            return null;

        string attributeText = $"[{attributeName.Replace(nameof(Attribute), "")}({sourceName})]";

        var returnType = source.GetSourceReturnType();
        string prefix = $"Unable to use {owner} with {attributeText} because {sourceType.Name}.{sourceName} returns "
                      + $"{returnType.GetCorrectCSharpTypeName(includeNamespace: false, includeGenericArgumentsNamespace: false, prefixWithGlobal: false)}";

        return returnType.CountSourceShapes() switch
        {
            0 => new ValidationError(TreatsWarningsAsErrors,
                $"{prefix}, which is neither IEnumerable<T> nor IAsyncEnumerable<T>. The non-generic IEnumerable is not enough on its own. Please, return IEnumerable<T> or IAsyncEnumerable<T>."),
            > 1 => new ValidationError(TreatsWarningsAsErrors,
                $"{prefix}, which has more than one enumerable shape, so BenchmarkDotNet cannot tell which one to read the values from. Please, return a single IEnumerable<T> or IAsyncEnumerable<T>."),
            _ => null
        };
    }
}
