using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using System.Reflection;

namespace BenchmarkDotNet.Validators;

/// <summary>
/// Validates that every `required` member of a benchmark type is one BenchmarkDotNet can set when it
/// constructs the type. The runtime counterpart of the BDN1109 and BDN1110 analyzer rules.
/// </summary>
public class RequiredMemberValidator : IValidator
{
    public static readonly RequiredMemberValidator FailOnError = new();

    // Emitted by the compiler; referenced by name so this also works on target frameworks without the types.
    private const string RequiredMemberAttributeFullName = "System.Runtime.CompilerServices.RequiredMemberAttribute";
    private const string SetsRequiredMembersAttributeFullName = "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute";

    public bool TreatsWarningsAsErrors => true;

    public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters input) => input.Benchmarks
        .Select(benchmark => benchmark.Descriptor.Type)
        .Distinct()
        .SelectMany(ValidateAsync)
        .ToAsyncEnumerable();

    private IEnumerable<ValidationError> ValidateAsync(Type type)
    {
        const BindingFlags reflectionFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // The generated runnable derives from the benchmark type, so its constructor chains to this one. C# would
        // force that constructor to repeat [SetsRequiredMembers] (CS9039), which suppresses required-member
        // checking entirely and would silently hide required members BenchmarkDotNet cannot set.
        var constructor = type.GetConstructor(reflectionFlags, binder: null, Type.EmptyTypes, modifiers: null);
        if (constructor != null && HasAttribute(constructor, SetsRequiredMembersAttributeFullName))
            yield return new ValidationError(TreatsWarningsAsErrors,
                $"Unable to use {type.Name} because its constructor is annotated with [SetsRequiredMembers], which BenchmarkDotNet's generated constructor cannot honor. Please, remove the attribute and set each required member with [Params], [ParamsSource], [ParamsAllValues] or [BenchmarkCancellation].");

        // Only fields and properties can be `required`. Note the attribute is also stamped on any *type* that
        // declares required members, so a nested type would look required if we asked for all members.
        var members = type.GetFields(reflectionFlags).Cast<MemberInfo>().Concat(type.GetProperties(reflectionFlags));

        foreach (var memberInfo in members)
        {
            if (!HasAttribute(memberInfo, RequiredMemberAttributeFullName) || IsSetByBenchmarkDotNet(memberInfo))
                continue;

            yield return new ValidationError(TreatsWarningsAsErrors,
                $"Unable to use {type.Name}.{memberInfo.Name} because it's a required member that BenchmarkDotNet cannot set. Please, remove the 'required' modifier or annotate it with [Params], [ParamsSource], [ParamsAllValues] or [BenchmarkCancellation].");
        }
    }

    // BenchmarkDotNet assigns these when it constructs the benchmark, so they satisfy the `required` modifier.
    private static bool IsSetByBenchmarkDotNet(MemberInfo member)
        => member.ResolveAttribute<ParamsAttribute>() != null
        || member.ResolveAttribute<ParamsSourceAttribute>() != null
        || member.ResolveAttribute<ParamsAllValuesAttribute>() != null
        || member.ResolveAttribute<BenchmarkCancellationAttribute>() != null;

    private static bool HasAttribute(MemberInfo member, string attributeFullName)
        => member.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == attributeFullName);
}
