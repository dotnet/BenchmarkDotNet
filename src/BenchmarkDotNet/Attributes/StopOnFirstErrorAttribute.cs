﻿using System;
using BenchmarkDotNet.Configs;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes {
    /// <summary>
    /// determines if running should be stop after first error
    /// </summary>
    [PublicAPI]
    public class StopOnFirstErrorAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        public StopOnFirstErrorAttribute(bool value = true)
        {
            Config = ManualConfig.CreateEmpty().StopOnFirstError(value);
        }
    }
}