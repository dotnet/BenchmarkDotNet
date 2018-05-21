using System;
using System.Linq;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Code
{
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

        public string DisplayText => $"Array[{array.Length}]";

        public string ToSourceCode()
            => $"new[] {{ {string.Join(", ", array.Select(item => toSourceCode?.Invoke(item) ?? SourceCodeHelper.ToSourceCode(item)))} }}";

        /// <summary>
        /// for types where calling .ToString() will be enough to re-create them in auto-generated source code file (integers, strings and other primitives)
        /// </summary>
        public static ArrayParam<T> ForPrimitives(T[] array) => new ArrayParam<T>(array);

        /// <summary>
        /// for types where calling .ToString() will be NOT enough to re-create them in auto-generated source code file
        /// </summary>
        /// <param name="array">the array</param>
        /// <param name="toSourceCode">method which tranforms an item of type T to it's C# representation
        /// example: point => $"new Point2d({point.X}, {point.Y})"
        /// </param>
        public static ArrayParam<T> ForComplexTypes(T[] array, Func<T, string> toSourceCode) => new ArrayParam<T>(array, toSourceCode);
    }
}