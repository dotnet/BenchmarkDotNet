using BenchmarkDotNet.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Extensions
{
    internal static class ReflectionExtensions
    {
        internal static T? ResolveAttribute<T>(this Type? type) where T : Attribute =>
            type?.GetTypeInfo().GetCustomAttributes(typeof(T), false).OfType<T>().FirstOrDefault();

        internal static T? ResolveAttribute<T>(this MemberInfo? memberInfo) where T : Attribute =>
            memberInfo?.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

        internal static bool HasAttribute<T>(this MemberInfo? memberInfo) where T : Attribute =>
            memberInfo.ResolveAttribute<T>() != null;

        internal static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;

        public static bool IsInitOnly(this PropertyInfo propertyInfo)
        {
            var setMethodReturnParameter = propertyInfo.SetMethod?.ReturnParameter;
            if (setMethodReturnParameter == null)
                return false;

            var isExternalInitType = typeof(System.Runtime.CompilerServices.Unsafe).Assembly
                .GetType("System.Runtime.CompilerServices.IsExternalInit");
            if (isExternalInitType == null)
                return false;

            return setMethodReturnParameter.GetRequiredCustomModifiers().Contains(isExternalInitType);
        }

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

        internal static (string Name, TAttribute Attribute, bool IsStatic, Type ParameterType)[]
            GetTypeMembersWithGivenAttribute<TAttribute>(this Type type, BindingFlags reflectionFlags)
            where TAttribute : Attribute
        {
            var fields = type
                .GetFields(reflectionFlags)
                .Select(f => Create(
                    f.Name,
                    f.ResolveAttribute<TAttribute>(),
                    f.IsStatic,
                    f.FieldType));

            var properties = type
                .GetProperties(reflectionFlags)
                .Select(p => Create(
                    p.Name,
                    p.ResolveAttribute<TAttribute>(),
                    p.GetSetMethod()?.IsStatic == true,
                    p.PropertyType));

            return fields.Concat(properties)
                .WhereNotNull()
                .Select(x => x!.Value)
                .ToArray();

            static (string Name, TAttribute Attribute, bool IsStatic, Type MemberType)?
                Create(string name, TAttribute? attribute, bool isStatic, Type memberType)
            {
                if (attribute == null)
                    return null;
                return (name, attribute, isStatic, memberType);
            }
        }

        internal static bool IsStackOnlyWithImplicitCast(this Type argumentType, [NotNullWhen(true)] object? argumentInstance)
        {
            if (argumentInstance == null)
                return false;

            if (!argumentType.IsByRefLike())
                return false;

            var instanceType = argumentInstance.GetType();

            var implicitCastsDefinedInArgumentInstance = instanceType.GetMethods().Where(method => method.Name == "op_Implicit" && method.GetParameters().Any()).ToArray();
            if (implicitCastsDefinedInArgumentInstance.Any(implicitCast => implicitCast.ReturnType == argumentType && implicitCast.GetParameters().All(p => p.ParameterType == instanceType)))
                return true;

            var implicitCastsDefinedInArgumentType = argumentType.GetMethods().Where(method => method.Name == "op_Implicit" && method.GetParameters().Any()).ToArray();
            if (implicitCastsDefinedInArgumentType.Any(implicitCast => implicitCast.ReturnType == argumentType && implicitCast.GetParameters().All(p => p.ParameterType == instanceType)))
                return true;

            return false;
        }

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
            // 1. Pattern first: a public instance GetAsyncEnumerator with all-optional parameters whose
            //    return type has a public instance MoveNextAsync awaitable-to-bool (also accepting
            //    all-optional params) and a public instance Current property. Roslyn's `await foreach`
            //    binds to this in preference to the interface, so we mirror that order. The element type
            //    comes from Current so it tracks what the compiler binds to, even if the type also
            //    implements IAsyncEnumerable<U> for a different U. (Extension GetAsyncEnumerator is not
            //    handled.)
            //
            //    Note: when the type IS exactly IAsyncEnumerable<T>, `GetMethods(Public|Instance)` returns
            //    the interface's own GetAsyncEnumerator, so this branch also handles that case naturally —
            //    we just flag it as interface dispatch via the conditional below.
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
                    var ifaceItemType = iface.GetGenericArguments()[0];
                    var ifaceEnumeratorType = typeof(IAsyncEnumerator<>).MakeGenericType(ifaceItemType);
                    var ifaceMoveNextAsync = ifaceEnumeratorType.GetMethod(nameof(IAsyncEnumerator<>.MoveNextAsync))!;
                    // `MoveNextAsync` on `IAsyncEnumerator<T>` returns `ValueTask<bool>` which always
                    // satisfies the awaitable shape; pull the resolved `AwaitableInfo` from IsAwaitable
                    // rather than constructing it by hand.
                    ifaceMoveNextAsync.ReturnType.IsAwaitable(out var ifaceMoveNextAwaitable);
                    info = new AsyncEnumerableInfo(
                        ifaceItemType,
                        ifaceEnumeratorType,
                        iface.GetMethod(nameof(IAsyncEnumerable<>.GetAsyncEnumerator))!,
                        ifaceMoveNextAsync,
                        ifaceMoveNextAwaitable!,
                        ifaceEnumeratorType.GetProperty(nameof(IAsyncEnumerator<>.Current))!,
                        IsInterfaceDispatch: true);
                    return true;
                }
            }
            info = null;
            return false;
        }

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