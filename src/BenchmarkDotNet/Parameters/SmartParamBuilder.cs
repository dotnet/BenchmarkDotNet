﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Parameters
{
    internal static class SmartParamBuilder
    {
        [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
        internal static object[] CreateForParams(MemberInfo source, object[] values)
        {
            if (values.IsEmpty() || values.All(SourceCodeHelper.IsCompilationTimeConstant))
                return values;

            return values.Select((value, index) => new SmartParameter(source, value, index)).ToArray();
        }

        internal static ParameterInstances CreateForArguments(MethodInfo benchmark, ParameterDefinition[] parameterDefinitions, (MemberInfo source, object[] values) valuesInfo, int sourceIndex, SummaryStyle summaryStyle)
        {
            var unwrappedValue = valuesInfo.values[sourceIndex];

            if (unwrappedValue is object[] array)
            {
                // the user provided object[] for a benchmark accepting a single argument
                if (parameterDefinitions.Length == 1 && array.Length == 1
                    && array[0].GetType() == benchmark.GetParameters().FirstOrDefault()?.ParameterType) // the benchmark that accepts an object[] as argument
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

        public string DisplayText => Value is Array array ? ArrayParam.GetDisplayString(array) : Value.ToString();

        public string ToSourceCode()
        {
            string cast = $"({parameterDefinitions[argumentIndex].ParameterType.GetCorrectCSharpTypeName()})"; // it's an object so we need to cast it to the right type

            string callPostfix = source is PropertyInfo ? string.Empty : "()";

            string indexPostfix = parameterDefinitions.Length > 1
                ? $"[{argumentIndex}]" // IEnumerable<object[]>
                : string.Empty; // IEnumerable<object>

            // we just execute (cast)source.ToArray()[case][argumentIndex];
            // we know that source is IEnumerable so we can do that!
            return $"{cast}System.Linq.Enumerable.ToArray({source.Name}{callPostfix})[{sourceIndex}]{indexPostfix};";
        }
    }

    internal class SmartParameter : IParam
    {
        private readonly MemberInfo source;
        private readonly MethodBase method;
        private readonly int index;

        public SmartParameter(MemberInfo source, object value, int index)
        {
            this.source = source;
            method = source is PropertyInfo property ? property.GetMethod : source as MethodInfo;
            Value = value;
            this.index = index;
        }

        public object Value { get; }

        public string DisplayText => Value is Array array ? ArrayParam.GetDisplayString(array) : Value.ToString();

        public string ToSourceCode()
        {
            string cast = $"({Value.GetType().GetCorrectCSharpTypeName()})";

            string instancePrefix = method.IsStatic ? source.DeclaringType.GetCorrectCSharpTypeName() : "instance";

            string callPostfix = source is PropertyInfo ? string.Empty : "()";

            // we just execute (cast)source.ToArray()[index];
            return $"{cast}System.Linq.Enumerable.ToArray({instancePrefix}.{source.Name}{callPostfix})[{index}];";
        }
    }
}
