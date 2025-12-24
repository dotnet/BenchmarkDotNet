using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;

namespace BenchmarkDotNet.Parameters
{
    internal static class SmartParamBuilder
    {
        [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
        internal static object[] CreateForParams(Type parameterType, MemberInfo source, object[] values)
        {
            // IEnumerable<object>
            if (values.IsEmpty() || values.All(SourceCodeHelper.IsCompilationTimeConstant))
                return values;

            // IEnumerable<object[]>
            if (values.All(value => value is object[] array && array.Length == 1 && SourceCodeHelper.IsCompilationTimeConstant(array[0])))
                return values.Select(x => ((object[])x)[0]).ToArray();

            return values.Select((value, index) => new SmartParameter(parameterType, source, value, index)).ToArray();
        }

        internal static ParameterInstances CreateForArguments(MethodInfo benchmark, ParameterDefinition[] parameterDefinitions, (MemberInfo source, object[] values) valuesInfo, int sourceIndex, SummaryStyle summaryStyle)
        {
            var unwrappedValue = valuesInfo.values[sourceIndex];

            if (unwrappedValue is object[] array)
            {
                Type firstParamType = benchmark.GetParameters().FirstOrDefault()?.ParameterType;
                // the user provided object[] for a benchmark accepting a single argument
                if (parameterDefinitions.Length == 1 && array.Length == 1
                    && (array[0]?.GetType() == firstParamType || (firstParamType != null && firstParamType.IsStackOnlyWithImplicitCast(array[0])))) // the benchmark that accepts an object[] as argument
                {
                    return new ParameterInstances(
                        new[] { Create(parameterDefinitions, array[0], valuesInfo.source, sourceIndex, argumentIndex: 0, summaryStyle) });
                }

                if (parameterDefinitions.Length > 1)
                {
                    if (parameterDefinitions.Length != array.Length)
                        throw new InvalidOperationException($"Benchmark {benchmark.Name} has invalid number of arguments provided by [ArgumentsSource({valuesInfo.source.Name})]! {array.Length} instead of {parameterDefinitions.Length}.");

                    return new ParameterInstances(
                        array.Select((value, argumentIndex) => Create(parameterDefinitions, value, valuesInfo.source, sourceIndex, argumentIndex, summaryStyle)).ToArray());
                }
            }

            if (parameterDefinitions.Length == 1)
            {
                return new ParameterInstances(
                    new[] { Create(parameterDefinitions, unwrappedValue, valuesInfo.source, sourceIndex, argumentIndex: 0, summaryStyle) });
            }

            throw new NotSupportedException($"Benchmark {benchmark.Name} has invalid type of arguments provided by [ArgumentsSource({valuesInfo.source.Name})]. It should be IEnumerable<object[]> or IEnumerable<object>.");
        }

        private static ParameterInstance Create(ParameterDefinition[] parameterDefinitions, object value, MemberInfo source, int sourceIndex, int argumentIndex, SummaryStyle summaryStyle)
        {
            if (SourceCodeHelper.IsCompilationTimeConstant(value))
                return new ParameterInstance(parameterDefinitions[argumentIndex], value, summaryStyle);

            return new ParameterInstance(parameterDefinitions[argumentIndex], new SmartArgument(parameterDefinitions, value, source, sourceIndex, argumentIndex), summaryStyle);
        }
    }

    internal class SmartArgument : IParam
    {
        private readonly ParameterDefinition[] parameterDefinitions;
        private readonly MemberInfo source;
        private readonly int sourceIndex;
        private readonly int argumentIndex;

        public SmartArgument(ParameterDefinition[] parameterDefinitions, object value, MemberInfo source, int sourceIndex, int argumentIndex)
        {
            this.parameterDefinitions = parameterDefinitions;
            Value = value;
            this.source = source;
            this.sourceIndex = sourceIndex;
            this.argumentIndex = argumentIndex;
        }

        public object Value { get; }

        public string DisplayText => Value is Array array ? ArrayParam.GetDisplayString(array) : Value?.ToString() ?? ParameterInstance.NullParameterTextRepresentation;

        public string ToSourceCode()
        {
            Type paramType = parameterDefinitions[argumentIndex].ParameterType;

            // it's an object so we need to cast it to the right type
            string cast = paramType.IsByRefLike()
                ? $"({Value.GetType().GetCorrectCSharpTypeName()})"
                : $"({paramType.GetCorrectCSharpTypeName()})";

            string callPostfix = source is PropertyInfo ? string.Empty : "()";

            MethodInfo sourceAsMethodInfo =  source as MethodInfo;
            PropertyInfo sourceAsPropertyInfo = source as PropertyInfo;

            Type indexableType = typeof(IEnumerable<object[]>);

            string indexPostfix;
            if (sourceAsMethodInfo?.ReturnType == indexableType ||
                sourceAsPropertyInfo?.GetMethod.ReturnType == indexableType) {
                indexPostfix = $"[{argumentIndex}]";
            }
            else
            {
                indexPostfix = string.Empty; // IEnumerable<object>
            }

            string methodCall;
            if (sourceAsMethodInfo?.IsStatic ?? sourceAsPropertyInfo?.GetMethod.IsStatic ?? throw new Exception($"{nameof(source)} was not {nameof(MethodInfo)} nor {nameof(PropertyInfo)}"))
            {
                // If the source member is static, we need to place the fully qualified type name before it, in case the source member is from another type that this generated type does not inherit from.
                methodCall = $"{source.DeclaringType.GetCorrectCSharpTypeName()}.{source.Name}";
            }
            else
            {
                // If the source member is non-static, we mustn't include the type name, as this would be a compiler error when accessing a non-static source member in the base class of this generated type.
                methodCall = $"base.{source.Name}";
            }

            // we do something like enumerable.ElementAt(sourceIndex)[argumentIndex];
            return $"{cast}BenchmarkDotNet.Parameters.ParameterExtractor.GetParameter({methodCall}{callPostfix}, {sourceIndex}){indexPostfix};";
        }
    }

    internal class SmartParameter : IParam
    {
        private readonly Type parameterType;
        private readonly MemberInfo source;
        private readonly MethodBase method;
        private readonly int index;

        public SmartParameter(Type parameterType, MemberInfo source, object value, int index)
        {
            this.parameterType = parameterType;
            this.source = source;
            method = source is PropertyInfo property ? property.GetMethod : source as MethodInfo;
            Value = value;
            this.index = index;
        }

        public object Value { get; }

        public string DisplayText => Value is Array array ? ArrayParam.GetDisplayString(array) : Value?.ToString() ?? ParameterInstance.NullParameterTextRepresentation;

        public string ToSourceCode()
        {
            string cast = $"({parameterType.GetCorrectCSharpTypeName()})"; // it's an object so we need to cast it to the right type

            string callPrefix = method.IsStatic ? source.DeclaringType.GetCorrectCSharpTypeName() : "base";

            string callPostfix = source is PropertyInfo ? string.Empty : "()";

            // we so something like enumerable.ElementAt(index);
            return $"{cast}BenchmarkDotNet.Parameters.ParameterExtractor.GetParameter({callPrefix}.{source.Name}{callPostfix}, {index});";
        }
    }

    public static class ParameterExtractor
    {
        [EditorBrowsable(EditorBrowsableState.Never)] // hide from intellisense, it's public so we can call it form the boilerplate code
        public static T GetParameter<T>(IEnumerable<T> parameters, int index)
        {
            int count = 0;

            foreach (T parameter in parameters)
            {
                if (count == index)
                {
                    return parameter;
                }

                if (parameter is IDisposable disposable)
                {
                    // parameters might contain locking finalizers which might cause the benchmarking process to hung at the end
                    // to avoid that, we dispose the parameters that were created, but won't be used
                    // (for every test case we have to enumerate the underlying source enumerator and stop when we reach index of given test case)
                    // See https://github.com/dotnet/BenchmarkDotNet/issues/1383 and https://github.com/dotnet/runtime/issues/314 for more
                    disposable.Dispose();
                }

                count++;
            }

            throw new InvalidOperationException("We should never get here!");
        }
    }
}
