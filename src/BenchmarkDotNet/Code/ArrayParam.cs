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
            => $"{array.GetType().GetElementType()?.GetDisplayName()}[{array.Length}]";

        public static string GetValueString(Array array)
            => $"{GetDisplayString(array)} (rank {array.Rank}, hash {GetArrayValueHash(array)})";

        private static int GetArrayValueHash(Array array)
        {
            var arrFlat = array.Cast<object>();

            int hash = 0;

            foreach (var elem in arrFlat)
            {
                hash = HashCode.Combine(hash, elem);
            }

            return hash;
        }
    }

    public class ArrayParam<T> : IParam
    {
        private readonly T[] array;
        private readonly Func<T, string>? toSourceCode;

        private ArrayParam(T[] array, Func<T, string>? toSourceCode = null)
        {
            this.array = array;
            this.toSourceCode = toSourceCode;
        }

        public object Value => array;

        public string DisplayText => ArrayParam.GetDisplayString(array);

        public string ValueText => ArrayParam.GetValueString(array);

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

        internal static IParam? FromObject(object array)
        {
            var type = array.GetType();
            if (!type.IsArray)
                throw new InvalidOperationException("The argument must be an array");
            var elementType = type.GetElementType();
            if (elementType == null)
                throw new InvalidOperationException("Failed to determine type of array elements");
            if (!SourceCodeHelper.IsCompilationTimeConstant(elementType))
                throw new InvalidOperationException("The argument must be an array of primitives");

            var arrayParamType = typeof(ArrayParam<>).MakeGenericType(elementType);

            var methodInfo = arrayParamType.GetMethod(nameof(ForPrimitives), BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException($"{nameof(ForPrimitives)} not found");
            return (IParam?)methodInfo.Invoke(null, new[]{ array});
        }
    }
}