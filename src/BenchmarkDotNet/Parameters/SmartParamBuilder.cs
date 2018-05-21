﻿using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Parameters
{
    static internal class SmartParamBuilder
    {
        internal static object[] CreateForParams(MemberInfo source, object[] values)
        {
            if (values.IsEmpty() || values.All(value => SourceCodeHelper.IsCompilationTimeConstant(value)))
                return values;

            return values.Select((value, index) => new SmartParamameter(source, value, index)).ToArray();
        }

        internal static ParameterInstances CreateForArguments(MethodInfo benchmark, ParameterDefinition[] parameterDefinitions, (MemberInfo source, object[] values) valuesInfo, int sourceIndex)
        {
            var unwrappedValue = valuesInfo.values[sourceIndex];

            if (unwrappedValue is object[] array)
            {
                if (parameterDefinitions.Length != array.Length)
                    throw new InvalidOperationException($"Benchmark {benchmark.Name} has invalid number of arguments provided by [ArgumentsSource({valuesInfo.source.Name})]! {array.Length} instead of {parameterDefinitions.Length}.");

                return new ParameterInstances(
                    array.Select((value, argumentIndex) => Create(parameterDefinitions, value, valuesInfo.source, sourceIndex, argumentIndex)).ToArray());
            }
            else if (parameterDefinitions.Length == 1)
            {
                return new ParameterInstances(
                    new[] { Create(parameterDefinitions, unwrappedValue, valuesInfo.source, sourceIndex, argumentIndex: 0) });
            }
            else throw new NotSupportedException($"Benchmark {benchmark.Name} has invalid type of arguments provided by [ArgumentsSource({valuesInfo.source.Name})]. It should be IEnumerable<object[]> or IEnumerable<object>.");
        }

        private static ParameterInstance Create(ParameterDefinition[] parameterDefinitions, object value, MemberInfo source, int sourceIndex, int argumentIndex)
        {
            if (SourceCodeHelper.IsCompilationTimeConstant(value))
                return new ParameterInstance(parameterDefinitions[argumentIndex], value);

            return new ParameterInstance(parameterDefinitions[argumentIndex], new SmartArgument(parameterDefinitions, value, source, sourceIndex, argumentIndex));
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

        public string DisplayText => Value.ToString();

        public string ToSourceCode()
        {
            var cast = $"({Value.GetType().GetCorrectCSharpTypeNameWithoutRef()})"; // it's an object so we need to cast it to the right type

            var callPostfix = source is PropertyInfo ? string.Empty : "()";

            var indexPostfix = parameterDefinitions.Length > 1 
                ? $"[{argumentIndex}]" // IEnumerable<object[]> 
                : string.Empty; // IEnumerable<object>

            // we just execute (cast)source.ToArray()[case][argumentIndex]; 
            // we know that source is IEnumberable so we can do that!
            return $"{cast}{source.Name}{callPostfix}.ToArray()[{sourceIndex}]{indexPostfix};"; 
        }
    }

    internal class SmartParamameter : IParam
    {
        private readonly MemberInfo source;
        private readonly MethodBase method;
        private readonly int index;

        public SmartParamameter(MemberInfo source, object value, int index)
        {
            this.source = source;
            this.method = source is PropertyInfo property ? property.GetMethod : source as MethodInfo;
            Value = value;
            this.index = index;
        }

        public object Value { get; }

        public string DisplayText => Value.ToString();

        public string ToSourceCode()
        {
            var cast = $"({Value.GetType().GetCorrectCSharpTypeNameWithoutRef()})";

            var instancePrefix = method.IsStatic ? source.DeclaringType.GetCorrectCSharpTypeNameWithoutRef() : "instance";

            var callPostfix = source is PropertyInfo ? string.Empty : "()";

            // we just execute (cast)source.ToArray()[index]; 
            return $"{cast}{instancePrefix}.{source.Name}{callPostfix}.ToArray()[{index}];";
        }
    }
}
