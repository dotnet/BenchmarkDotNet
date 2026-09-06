using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Extensions;
using System.Reflection;

namespace BenchmarkDotNet.Validators
{
    public class ParamsValidator : IValidator
    {
        public static readonly ParamsValidator FailOnError = new();

        public bool TreatsWarningsAsErrors => true;

        public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters input) => input.Benchmarks
            .Select(benchmark => benchmark.Descriptor.Type)
            .Distinct()
            .SelectMany(ValidateAsync)
            .ToAsyncEnumerable();

        private static bool IsStatic(MemberInfo member) => member switch
        {
            FieldInfo field => field.IsStatic,
            PropertyInfo property => (property.GetMethod ?? property.SetMethod)!.IsStatic,
            _ => false
        };

        private IEnumerable<ValidationError> ValidateAsync(Type type)
        {
            foreach (var memberInfo in type.GetMembers(ReflectionExtensions.ParameterMemberFlags))
            {
                var attributes = new Attribute?[]
                    {
                        memberInfo.ResolveAttribute<ParamsAttribute>(),
                        memberInfo.ResolveAttribute<ParamsAllValuesAttribute>(),
                        memberInfo.ResolveAttribute<ParamsSourceAttribute>()
                    }
                    .WhereNotNull()
                    .ToList();
                if (attributes.IsEmpty())
                    continue;

                string name = $"{type.Name}.{memberInfo.Name}";
                string attributeString = string.Join(", ", attributes.Select(attribute => $"[{attribute.GetType().Name.Replace(nameof(Attribute), "")}]"));

                // The runnable derives from the benchmark type and assigns each instance parameter member through an
                // object initializer, which binds the member name unqualified. A parameter member named like a
                // generated member (all __-prefixed) binds to the generated member instead and fails to compile.
                // Static members are assigned fully type-qualified, so they cannot collide.
                if (!IsStatic(memberInfo) && RunnableConstants.ReservedInstanceMemberNames.Contains(memberInfo.Name))
                    yield return new ValidationError(TreatsWarningsAsErrors,
                        $"Unable to use {name} with {attributeString} because '{memberInfo.Name}' is a reserved name used by BenchmarkDotNet's code generation. Please, rename the member.");

                if (attributes.Count > 1)
                    yield return new ValidationError(TreatsWarningsAsErrors,
                        $"Unable to use {name} with {attributeString} at the same time. Please, use a single attribute.");

                if (memberInfo is FieldInfo fieldInfo)
                {
                    if (fieldInfo.IsLiteral || fieldInfo.IsInitOnly)
                    {
                        string modifier = fieldInfo.IsInitOnly ? "readonly" : "constant";
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because it's a {modifier} field. Please, remove the {modifier} modifier.");
                    }

                    if (!fieldInfo.IsPublic)
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because it's not public. Please, make it public.");
                }

                if (memberInfo is PropertyInfo propertyInfo)
                {
                    // An init-only setter is fine: the runnable assigns parameters through an object initializer.
                    if (propertyInfo.SetMethod == null)
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because it has no setter. Please, provide a public setter.");

                    if (propertyInfo.SetMethod != null && !propertyInfo.SetMethod.IsPublic)
                        yield return new ValidationError(TreatsWarningsAsErrors,
                            $"Unable to use {name} with {attributeString} because its setter is not public. Please, make the setter public.");
                }
            }
        }
    }
}