using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public class BenchmarkCategoryAttribute : Attribute
    {
        public string[] Categories { get; }

        // CLS-Compliant Code requires a constuctor without an array in the argument list
        protected BenchmarkCategoryAttribute() { }

        public BenchmarkCategoryAttribute(params string[] categories)
        {
            Categories = categories;
        }
    }
}