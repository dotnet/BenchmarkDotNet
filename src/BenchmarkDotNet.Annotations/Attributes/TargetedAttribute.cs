﻿using System;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Base class for attributes that are targeted at one or more method(s)
    /// </summary>
    public abstract class TargetedAttribute : Attribute
    {
        public string[] Targets { get; set; }

        /// <summary>
        /// Target method for attribute
        /// </summary>
        public string Target
        {
            set => Targets = string.IsNullOrEmpty(value) ? new string[0] : value.Split(','); // , is for backward compat
            get => throw new InvalidOperationException("Please use Targets property"); // kept to keep compiler happy "Named attribute arguments must be fields which are not readonly, static, or const, or read-write properties which are public and not static."
        }

        public bool Match(MethodInfo method) => Targets == null || Targets.Length == 0 || Targets.Contains(method.Name);
    }
}
