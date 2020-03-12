using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Code
{
    internal static class ArrayParam
    {
        public static string GetDisplayString(Array array)
            => $"{array.GetType().GetElementType().GetDisplayName()}[{array.Length}]";
    }

    public class ArrayParam<T> : IParam
    {
        private readonly T[] array;
        private readonly Func<T, string> toSourceCode;

        private ArrayParam(T[] array, Func<T, string> toSourceCode = null)
        {
            this.array = array;
            this.toSourceCode = toSourceCode;
        }

        public object Value => array;

        public string DisplayText => ArrayParam.GetDisplayString(array);

        public string ToSourceCode()
            => $"new {typeof(T).GetCorrectCSharpTypeName()}[] {{ {string.Join(", ", array.Select(item => toSourceCode?.Invoke(item) ?? SourceCodeHelper.ToSourceCode(item)))} }}";

        /// <summary>
        /// for types where calling .ToString() will be enough to re-create them in auto-generated source code file (integers, strings and other primitives)
        /// </summary>
        public static ArrayParam<T> ForPrimitives(T[] array) => new ArrayParam<T>(array);

        /// <summary>
        /// for types where calling .ToString() will be NOT enough to re-create them in auto-generated source code file
        /// </summary>
        /// <param name="array">the array</param>
        /// <param name="toSourceCode">method which transforms an item of type T to it's C# representation
        /// example: point => $"new Point2d({point.X}, {point.Y})"
        /// </param>
        [PublicAPI] public static ArrayParam<T> ForComplexTypes(T[] array, Func<T, string> toSourceCode) => new ArrayParam<T>(array, toSourceCode);

        internal static IParam FromObject(object array)
        {
            var type = array.GetType();
            if (!type.IsArray)
                throw new InvalidOperationException("The argument must be an array");
            if (!SourceCodeHelper.IsCompilationTimeConstant(type.GetElementType()))
                throw new InvalidOperationException("The argument must be an array of primitives");

            var arrayParamType = typeof(ArrayParam<>).MakeGenericType(type.GetElementType());

            var methodInfo = arrayParamType.GetMethod(nameof(ForPrimitives), BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException($"{nameof(ForPrimitives)} not found");
            return (IParam)methodInfo.Invoke(null, new[]{ array});
        }
    }
}