using BenchmarkDotNet.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Extensions
{
    internal static class ReflectionExtensions
    {
        // The name the compiler gives an `implicit operator`; there is no reflection API that spells it.
        internal const string OpImplicitMethodName = "op_Implicit";

        internal static T? ResolveAttribute<T>(this Type? type) where T : Attribute =>
            type?.GetTypeInfo().GetCustomAttributes(typeof(T), false).OfType<T>().FirstOrDefault();

        internal static T? ResolveAttribute<T>(this MemberInfo? memberInfo) where T : Attribute =>
            memberInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        internal static bool HasAttribute<T>(this MemberInfo? memberInfo) where T : Attribute =>
            memberInfo.ResolveAttribute<T>() != null;

        /// <summary>
        /// The value to pass for an omitted optional argument. A parameter can be optional without declaring a
        /// default ([Optional] with no [DefaultParameterValue]); null is right for those, because Invoke converts it
        /// to default(T) even for a value type. Type.Missing is not: Invoke(object, object[]) does no
        /// optional-parameter binding and rejects it.
        /// </summary>
        internal static object? GetDefaultArgumentValue(this ParameterInfo parameter)
            => parameter.HasDefaultValue ? parameter.DefaultValue : null;

        internal static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;

        /// <summary>
        /// returns type name which can be used in generated C# code
        /// </summary>
        internal static string GetCorrectCSharpTypeName(this Type type, bool includeNamespace = true, bool includeGenericArgumentsNamespace = true, bool prefixWithGlobal = true)
        {
            while (!(type.IsPublic || type.IsNestedPublic) && type.BaseType != null)
                type = type.BaseType;

            // the reflection is missing information about types passed by ref (ie ref ValueTuple<int> is reported as NON generic type)
            if (type.IsByRef && !type.IsGenericType)
                type = type.GetElementType() ?? throw new NullReferenceException(nameof(type.GetElementType)); // https://github.com/dotnet/corefx/issues/29975#issuecomment-393134330

            if (type == typeof(void))
                return "void";
            if (type == typeof(void*))
                return "void*";

            string prefix = "";

            if (type.Namespace.IsNotBlank() && includeNamespace)
            {
                prefix += type.Namespace + ".";

                if (prefixWithGlobal)
                    prefix = $"global::{prefix}";
            }

            if (type.GetTypeInfo().IsGenericParameter)
                return type.Name;

            if (type.IsArray)
            {
                var typeName = GetCorrectCSharpTypeName(type.GetElementType()!, includeNamespace, includeGenericArgumentsNamespace, prefixWithGlobal);
                var parts = typeName.Split(['['], count: 2);

                string repr = parts[0] + '[' + new string(',', type.GetArrayRank() - 1) + ']';

                if (parts.Length == 2) return repr + '[' + parts[1];

                return repr;
            }

            return prefix + string.Join(".", GetNestedTypeNames(type, includeGenericArgumentsNamespace, prefixWithGlobal).Reverse());
        }

        // from most nested to least
        private static IEnumerable<string> GetNestedTypeNames(Type type, bool includeGenericArgumentsNamespace, bool prefixWithGlobal)
        {
            var allTypeParameters = new Stack<Type>(type.GetGenericArguments());

            Type currentType = type;
            while (currentType != null)
            {
                string name = currentType.Name.Replace("&", string.Empty);

                if (name.Contains('`'))
                {
                    var parts = name.Split('`');
                    var mainName = parts[0];
                    var parameterCount = int.Parse(parts[1]);

                    var typeParameters = Enumerable
                        .Range(0, parameterCount)
                        .Select(_ => allTypeParameters.Pop())
                        .Reverse();

                    var args = string.Join(", ", typeParameters.Select(T => GetCorrectCSharpTypeName(T, includeGenericArgumentsNamespace, includeGenericArgumentsNamespace, prefixWithGlobal)));
                    name = $"{mainName}<{args}>";
                }

                yield return name;
                currentType = currentType.DeclaringType!;
            }
        }

        /// <summary>
        /// returns simple, human friendly display name
        /// </summary>
        internal static string GetDisplayName(this Type type) => GetDisplayName(type.GetTypeInfo());

        /// <summary>
        /// Returns a display name per type, aligned with <paramref name="types"/>. When more than one
        /// type shares the same simple <see cref="GetDisplayName(Type)"/> (e.g. same class name in
        /// different namespaces), the colliding names are qualified with their namespace so they can be
        /// told apart; unambiguous names are left as-is.
        /// </summary>
        internal static string[] GetDisambiguatedDisplayNames(this IReadOnlyList<Type> types)
        {
            var simpleNames = new string[types.Count];
            for (int i = 0; i < types.Count; i++)
                simpleNames[i] = types[i].GetDisplayName();

            var ambiguousNames = new HashSet<string>(
                simpleNames.GroupBy(name => name).Where(group => group.Count() > 1).Select(group => group.Key),
                StringComparer.Ordinal);

            var displayNames = new string[types.Count];
            for (int i = 0; i < types.Count; i++)
            {
                string? fullName = types[i].FullName;
                displayNames[i] = ambiguousNames.Contains(simpleNames[i]) && !string.IsNullOrEmpty(fullName)
                    ? fullName
                    : simpleNames[i];
            }

            return displayNames;
        }

        /// <summary>
        /// returns simple, human friendly display name
        /// </summary>
        private static string GetDisplayName(this TypeInfo typeInfo)
        {
            if (!typeInfo.IsGenericType)
                return typeInfo.Name;

            string mainName = typeInfo.Name.Substring(0, typeInfo.Name.IndexOf('`'));
            string args = string.Join(", ", typeInfo.GetGenericArguments().Select(GetDisplayName).ToArray());
            return $"{mainName}<{args}>";
        }

        internal static IEnumerable<MethodInfo> GetAllMethods(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (typeInfo != null)
            {
                foreach (var methodInfo in typeInfo.DeclaredMethods)
                    yield return methodInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

        internal static IEnumerable<FieldInfo> GetAllFields(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (typeInfo != null)
            {
                foreach (var fieldInfo in typeInfo.DeclaredFields)
                    yield return fieldInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

        internal static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            while (typeInfo != null)
            {
                foreach (var propertyInfo in typeInfo.DeclaredProperties)
                    yield return propertyInfo;
                typeInfo = typeInfo.BaseType?.GetTypeInfo();
            }
        }

        internal static Type[] GetRunnableBenchmarks(this Assembly assembly)
            => assembly
                .GetTypes()
                .Where(type => type.ContainsRunnableBenchmarks())
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToArray();

        internal static bool ContainsRunnableBenchmarks(this Type type)
        {
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsAbstract || typeInfo.IsGenericType && !IsRunnableGenericType(typeInfo))
                return false;

            return typeInfo.GetBenchmarks().Any();
        }

        private static MethodInfo[] GetBenchmarks(this TypeInfo typeInfo)
            => typeInfo
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static) // we allow for Static now to produce a nice Validator warning later
                .Where(method => method.GetCustomAttributes(true).OfType<BenchmarkAttribute>().Any())
                .ToArray();

        internal static (string Name, TAttribute Attribute, bool IsStatic, Type ParameterType, MemberInfo Member)[]
            GetTypeMembersWithGivenAttribute<TAttribute>(this Type type, BindingFlags reflectionFlags)
            where TAttribute : Attribute
        {
            var fields = type
                .GetFields(reflectionFlags)
                .Select(f => Create(
                    f,
                    f.Name,
                    f.ResolveAttribute<TAttribute>(),
                    f.IsStatic,
                    f.FieldType));

            var properties = type
                .GetProperties(reflectionFlags)
                .Select(p => Create(
                    p,
                    p.Name,
                    p.ResolveAttribute<TAttribute>(),
                    p.GetSetMethod()?.IsStatic == true,
                    p.PropertyType));

            // One entry per name, keeping the most derived declaration. GetFields hands back a hidden base field
            // alongside the `new` one hiding it - GetProperties collapses the pair, so only fields arrive twice -
            // and the name binds to the most derived declaration everywhere it is then used. A second entry
            // becomes a second parameter of the same name: it multiplies the cases against itself and emits the
            // name twice in the runnable's object initializer, which is CS1912.
            var found = new List<(MemberInfo Member, string Name, TAttribute Attribute, bool IsStatic, Type MemberType)>();
            var indexByName = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var candidate in fields.Concat(properties).WhereNotNull().Select(x => x!.Value))
            {
                if (!indexByName.TryGetValue(candidate.Name, out int index))
                {
                    indexByName.Add(candidate.Name, found.Count);
                    found.Add(candidate);
                }
                else if (found[index].Member.DeclaringType!.IsAssignableFrom(candidate.Member.DeclaringType))
                {
                    found[index] = candidate;
                }
            }

            return found
                .Select(x => (x.Name, x.Attribute, x.IsStatic, x.MemberType, x.Member))
                .ToArray();

            static (MemberInfo Member, string Name, TAttribute Attribute, bool IsStatic, Type MemberType)?
                Create(MemberInfo member, string name, TAttribute? attribute, bool isStatic, Type memberType)
            {
                if (attribute == null)
                    return null;
                return (member, name, attribute, isStatic, memberType);
            }
        }

        // What a parameter takes, ref/in/out set aside: reflection reports those as byref types - `ref T` is `T&`,
        // which nothing is castable to - though the modifier says how the argument travels, not what it is.
        internal static Type WithoutRefModifier(this Type parameterType)
            => parameterType.IsByRef ? parameterType.GetElementType()! : parameterType;

        internal static bool IsStackOnlyWithImplicitCast(this Type argumentType, [NotNullWhen(true)] object? argumentInstance)
        {
            if (argumentInstance == null)
                return false;

            if (!argumentType.IsByRefLike())
                return false;

            var instanceType = argumentInstance.GetType();

            return HasImplicitConversion(argumentType, instanceType);
        }

        private static bool HasImplicitConversion(Type targetType, Type sourceType)
            => DeclaresConversion(sourceType, targetType, sourceType)
            || DeclaresConversion(targetType, targetType, sourceType);

        // An `implicit operator` written for exactly these types. Only exactly: a source is admitted by naming
        // what the parameter takes, so there is no conversion to reason about on the way into the operator - and
        // reasoning about one is what reflection cannot do, since it answers the CLR's rules rather than C#'s.
        //
        // C# gathers operators from both types and their base classes. Reflection withholds a base's statics
        // without FlattenHierarchy, so without it an operator inherited from a base is invisible.
        private static bool DeclaresConversion(Type declaringType, Type targetType, Type sourceType)
            => declaringType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Any(method => method.Name == OpImplicitMethodName
                    && method.ReturnType == targetType
                    && method.GetParameters() is { Length: 1 } parameters
                    && parameters[0].ParameterType == sourceType);

        private static bool IsRunnableGenericType(TypeInfo typeInfo)
            => // if it is an open generic - there must be GenericBenchmark attributes
                (!typeInfo.IsGenericTypeDefinition || typeInfo.GenericTypeArguments.Any() || typeInfo.GetCustomAttributes(true).OfType<GenericTypeArgumentsAttribute>().Any())
                    && typeInfo.DeclaredConstructors.Any(ctor => ctor.IsPublic && ctor.GetParameters().Length == 0); // we need public parameterless ctor to create it

        internal static bool IsLinqPad(this Assembly assembly) => assembly.FullName!.IndexOf("LINQPAD", StringComparison.OrdinalIgnoreCase) >= 0;

        internal static bool IsByRefLike(this Type type)
#if NETSTANDARD2_0
            // Type.IsByRefLike is not available in netstandard2.0.
            => type.IsValueType && type.CustomAttributes.Any(attr => attr.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute");
#else
            => type.IsByRefLike;
#endif

        internal static bool IsAwaitable(this Type type, [NotNullWhen(true)] out AwaitableInfo? info)
        {
            // This does not handle await extension.
            var getAwaiterMethod = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(Task.GetAwaiter) && m.GetParameters().Length == 0);
            if (getAwaiterMethod is null)
            {
                info = null;
                return false;
            }
            var awaiterType = getAwaiterMethod.ReturnType;
            var getResultMethod = awaiterType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(TaskAwaiter.GetResult) && m.GetParameters().Length == 0);
            var isCompletedProperty = awaiterType.GetProperty(nameof(TaskAwaiter.IsCompleted), BindingFlags.Public | BindingFlags.Instance);
            if (getResultMethod is null
                || isCompletedProperty?.PropertyType != typeof(bool)
                || !awaiterType.GetInterfaces().Any(t => typeof(INotifyCompletion).IsAssignableFrom(t)))
            {
                info = null;
                return false;
            }
            info = new AwaitableInfo(awaiterType, getAwaiterMethod, getResultMethod, isCompletedProperty, getResultMethod.ReturnType);
            return true;
        }

        internal static bool IsAsyncEnumerable(this Type type, [NotNullWhen(true)] out AsyncEnumerableInfo? info)
        {
            // 1. Pattern first, as `await foreach` binds: a public instance GetAsyncEnumerator with all-optional parameters,
            //    returning a type with a public MoveNextAsync awaitable-to-bool (also accepting all-optional params) and a
            //    public Current property. The element type comes from Current, so it tracks what the compiler binds to even
            //    when the type also implements IAsyncEnumerable<U> for another U. Extension GetAsyncEnumerator is not handled.
            //    IAsyncEnumerable<T> itself lands here too - GetMethods returns the interface's own - and the conditional
            //    below flags it as interface dispatch.
            var patternGetAsyncEnumerator = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(IAsyncEnumerable<>.GetAsyncEnumerator)
                    && m.GetParameters().All(p => p.IsOptional));
            if (patternGetAsyncEnumerator is not null)
            {
                var patternEnumeratorType = patternGetAsyncEnumerator.ReturnType;
                var moveNextAsyncMethod = patternEnumeratorType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == nameof(IAsyncEnumerator<>.MoveNextAsync) && m.GetParameters().All(p => p.IsOptional));
                if (moveNextAsyncMethod?.ReturnType.IsAwaitable(out var moveNextAwaitable) == true
                    && moveNextAwaitable.ResultType == typeof(bool)
                    && patternEnumeratorType.GetProperty(nameof(IAsyncEnumerator<>.Current), BindingFlags.Public | BindingFlags.Instance) is { } currentProperty)
                {
                    info = new AsyncEnumerableInfo(
                        currentProperty.PropertyType,
                        patternEnumeratorType,
                        patternGetAsyncEnumerator,
                        moveNextAsyncMethod,
                        moveNextAwaitable,
                        currentProperty,
                        IsInterfaceDispatch: type.IsInterface);
                    return true;
                }
                // A public pattern `GetAsyncEnumerator` was found but its return type doesn't satisfy
                // the await-foreach enumerator pattern. Roslyn commits to the pattern method once it's
                // found and reports an error rather than silently falling back to `IAsyncEnumerable<T>`,
                // so we reject here as well — even if the source also implements the interface.
                info = null;
                return false;
            }
            // 2. Fallback: no pattern method on the source — bind via the `IAsyncEnumerable<T>` interface
            //    if the source implements it (typically an explicit interface implementation).
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    info = GetAsyncEnumerableInterfaceInfo(iface.GetGenericArguments()[0]);
                    return true;
                }
            }
            info = null;
            return false;
        }

        // The await-foreach members of IAsyncEnumerable<T> itself, for a caller that has already established the
        // interface and wants it bound in preference to any pattern method the concrete type may also declare.
        // Every member is the interface's own, so invoking them dispatches to the implementation whether it is
        // implicit or explicit.
        internal static AsyncEnumerableInfo GetAsyncEnumerableInterfaceInfo(Type elementType)
        {
            var interfaceType = typeof(IAsyncEnumerable<>).MakeGenericType(elementType);
            var enumeratorType = typeof(IAsyncEnumerator<>).MakeGenericType(elementType);
            var moveNextAsync = enumeratorType.GetMethod(nameof(IAsyncEnumerator<>.MoveNextAsync))!;
            // `MoveNextAsync` on `IAsyncEnumerator<T>` returns `ValueTask<bool>` which always satisfies the
            // awaitable shape; pull the resolved `AwaitableInfo` from IsAwaitable rather than building it by hand.
            moveNextAsync.ReturnType.IsAwaitable(out var moveNextAwaitable);
            return new AsyncEnumerableInfo(
                elementType,
                enumeratorType,
                interfaceType.GetMethod(nameof(IAsyncEnumerable<>.GetAsyncEnumerator))!,
                moveNextAsync,
                moveNextAwaitable!,
                enumeratorType.GetProperty(nameof(IAsyncEnumerator<>.Current))!,
                IsInterfaceDispatch: true);
        }

        // Whether the type is, or implements, IAsyncEnumerable<T>. [ParamsSource]/[ArgumentsSource] async sources
        // must use the interface (not just the await-foreach pattern), so callers reject pattern-only types.
        internal static bool IsIAsyncEnumerable(this Type type, [NotNullWhen(true)] out Type? elementType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                {
                    elementType = iface.GetGenericArguments()[0];
                    return true;
                }
            }
            elementType = null;
            return false;
        }

        // Mirrors AsyncTypeShapes.CountSourceShapes on the analyzer side. The generated extraction call infers its
        // element type from the source, and type inference needs a *unique* candidate interface, so a source with
        // anything other than exactly one instantiation across the two shapes fails to compile (CS0411) - even when
        // one element type converts to the other, as with IEnumerable<string> plus IEnumerable<object>.
        internal static int CountSourceShapes(this Type type)
            => type.CountInstantiations(typeof(IEnumerable<>)) + type.CountInstantiations(typeof(IAsyncEnumerable<>));

        private static int CountInstantiations(this Type type, Type interfaceDefinition)
        {
            var found = new HashSet<Type>();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == interfaceDefinition)
                found.Add(type);
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == interfaceDefinition)
                    found.Add(iface);
            }
            return found.Count;
        }

        // The element type a source declares, which is the type the generated extraction call returns and therefore
        // the type the generated code indexes into. Only an unambiguous shape has one - see CountSourceShapes.
        internal static bool TryGetSourceElementType(this Type sourceReturnType, [NotNullWhen(true)] out Type? elementType)
        {
            if (sourceReturnType.CountSourceShapes() == 1)
            {
                foreach (var candidate in sourceReturnType.GetInterfaces().Prepend(sourceReturnType))
                {
                    if (candidate.IsGenericType
                        && (candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                            || candidate.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)))
                    {
                        elementType = candidate.GetGenericArguments()[0];
                        return true;
                    }
                }
            }
            elementType = null;
            return false;
        }

        // The member a [ParamsSource]/[ArgumentsSource] name resolves to: a public method whose parameters are all
        // optional, else a property with a public getter. The single resolver - discovery reads its values from
        // whatever this returns and SourceReturnTypeValidator reports on the same member, so the two cannot judge
        // different members of the same name. A generic method definition is passed over rather than matched, so a
        // property of that name serves the name instead of an unusable method.
        internal static MemberInfo? FindSourceMember(this Type sourceType, string sourceName)
            => (MemberInfo?) sourceType.GetAllMethods()
                    .FirstOrDefault(method => method.Name == sourceName && method.IsPublic
                        && !method.IsGenericMethodDefinition
                        && method.GetParameters().All(parameter => parameter.IsOptional))
                ?? sourceType.GetAllProperties()
                    .FirstOrDefault(property => property.Name == sourceName && property.GetMethod?.IsPublic == true);

        /// <summary>
        /// The members a benchmark's parameters are looked for on. FlattenHierarchy is what reaches a base type's
        /// statics - without it reflection returns inherited *instance* members only. Discovery and every validator
        /// reporting on parameter members share this, so none can judge a member another cannot see. Not to be
        /// confused with <see cref="DeclaredMemberFlags"/>, which is its opposite.
        /// </summary>
        internal const BindingFlags ParameterMemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

        /// <summary>
        /// The property or field a parameter is written through, matched on the declared type discovery recorded as
        /// well as the name. These flags reach a hidden base member beside the one hiding it, and reflection returns
        /// members in no particular order, so the most derived declaration is chosen here. The choice spans both
        /// kinds: a field can hide a property, and looking for one kind first would find the base member of that
        /// kind and never reach the derived member that is the parameter.
        /// </summary>
        internal static MemberInfo? GetParameterMember(this Type type, string name, Type parameterType, BindingFlags flags)
        {
            MemberInfo? found = null;
            foreach (var member in type.GetMembers(flags))
            {
                var memberType = member switch
                {
                    // An indexer takes arguments and is never a parameter member.
                    PropertyInfo property when property.GetIndexParameters().Length == 0 => property.PropertyType,
                    FieldInfo field => field.FieldType,
                    _ => null
                };

                if (memberType != parameterType || member.Name != name)
                    continue;
                if (found is null || found.DeclaringType!.IsAssignableFrom(member.DeclaringType))
                    found = member;
            }
            return found;
        }

        // DeclaredOnly because the member sought is declared on the type being searched, and a metadata token is
        // unique only within a module. Inherited members are what ParameterMemberFlags exists to reach.
        private const BindingFlags DeclaredMemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        // The member as `contextType`'s generic definition names it - on that definition, or on the base in its
        // hierarchy that declares it. The base is read as the derived type writes it, `Base<T>` carrying the derived
        // type's own T, so inherited and locally declared members share type parameters. Null when it is elsewhere.
        internal static MemberInfo? GetDeclaredMemberIn(this MemberInfo member, Type contextType)
        {
            if (member.DeclaringType is not { } declaringType)
                return null;

            var declaringDefinition = declaringType.IsGenericType ? declaringType.GetGenericTypeDefinition() : declaringType;

            for (var candidate = contextType.IsGenericType ? contextType.GetGenericTypeDefinition() : contextType; candidate != null; candidate = candidate.BaseType)
            {
                if ((candidate.IsGenericType ? candidate.GetGenericTypeDefinition() : candidate) != declaringDefinition)
                    continue;

                return candidate.GetMembers(DeclaredMemberFlags)
                    .FirstOrDefault(inherited => inherited.MetadataToken == member.MetadataToken);
            }

            return null;
        }

        // The type a source member hands back, which is what the generated code infers the element type from.
        internal static Type GetSourceReturnType(this MemberInfo source)
            => source is PropertyInfo property ? property.GetMethod!.ReturnType : ((MethodInfo) source).ReturnType;

        internal static Attribute? GetAsyncMethodBuilderAttribute(this MemberInfo memberInfo)
            // AsyncMethodBuilderAttribute can come from any assembly, so we need to use reflection by name instead of searching for the exact type.
            => memberInfo.GetCustomAttributes(false).FirstOrDefault(attr => attr.GetType().FullName == typeof(AsyncMethodBuilderAttribute).FullName) as Attribute;

        internal static bool HasAsyncMethodBuilderAttribute(this MemberInfo memberInfo)
            => memberInfo.GetAsyncMethodBuilderAttribute() != null;
    }

    /// <summary>
    /// Everything <see cref="ReflectionExtensions.IsAwaitable"/> resolves while binding the
    /// awaitable pattern — bundled so callers (emitter, codegen, validators) reuse the same lookups
    /// instead of repeating the GetAwaiter/GetResult/IsCompleted reflection.
    /// </summary>
    internal sealed record AwaitableInfo(
        Type AwaiterType,
        MethodInfo GetAwaiterMethod,
        MethodInfo GetResultMethod,
        PropertyInfo IsCompletedProperty,
        Type ResultType);

    /// <summary>
    /// Everything <see cref="ReflectionExtensions.IsAsyncEnumerable"/> resolves while binding the await-foreach
    /// pattern — bundled so callers (emitter, codegen, validators) reuse the same lookups instead of
    /// repeating the pattern-vs-interface discrimination and the Current-property search. DisposeAsync
    /// is only needed by the emitter, so its resolution lives there to keep validator/codegen paths
    /// from paying for a lookup they don't use.
    /// </summary>
    internal sealed record AsyncEnumerableInfo(
        Type ItemType,
        Type EnumeratorType,
        MethodInfo GetAsyncEnumeratorMethod,
        MethodInfo MoveNextAsyncMethod,
        AwaitableInfo MoveNextAwaitable,
        PropertyInfo CurrentProperty,
        bool IsInterfaceDispatch);
}