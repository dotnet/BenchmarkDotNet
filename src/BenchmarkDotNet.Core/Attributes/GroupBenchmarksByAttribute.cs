﻿using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class GroupBenchmarksByAttribute: Attribute, IConfigSource
    {
        public IConfig Config { get; }

        // CLS-Compliant Code requires a constuctor without an array in the argument list
        protected GroupBenchmarksByAttribute()
        {
            Config = ManualConfig.CreateEmpty();
        }
        
        public GroupBenchmarksByAttribute(params BenchmarkLogicalGroupRule[] rules)
        {
            Config = ManualConfig.CreateEmpty().With(rules);
        }
    }
}