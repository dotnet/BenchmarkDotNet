// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityAttribute.cs">
//   Copyright (c) 2020 Johannes Deml. All rights reserved.
// </copyright>
// <author>
//   Johannes Deml
//   public@deml.io
// </author>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace BenchmarkDotNet.Attributes
{
    public abstract class PriorityAttribute : Attribute
    {
        /// <summary>
        /// Defines order of column in the same category.
        /// </summary>
        public int Priority { get; set; } = 0;
    }
}