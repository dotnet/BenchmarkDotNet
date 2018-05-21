using System;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Base class for attributes that are targeted at one method
    /// </summary>
    public abstract class TargetedAttribute : Attribute
    {
        /// <summary>
        /// Target method for attribute
        /// </summary>
        public string Target { get; set; }
    }
}
