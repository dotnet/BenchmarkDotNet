using BenchmarkDotNet.Configs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AutoHideColumnAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        // CLS-Compliant Code requires a constructor without an array in the argument list
        protected AutoHideColumnAttribute() => Config = ManualConfig.CreateEmpty();

        public AutoHideColumnAttribute(params string[] names) => Config = ManualConfig.CreateEmpty().HideColumns(names);
    }
}
