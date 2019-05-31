using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public class BenchmarkCategoryAttribute : Attribute
    {
        public string[] Categories { get; }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        [PublicAPI] protected BenchmarkCategoryAttribute() { }

        public BenchmarkCategoryAttribute(params string[] categories) => Categories = categories;
    }
}