using System;

namespace BenchmarkDotNet.Attributes
{
    public abstract class PriorityAttribute : Attribute
    {
        /// <summary>
        /// Defines display order of column in the same category.
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}
