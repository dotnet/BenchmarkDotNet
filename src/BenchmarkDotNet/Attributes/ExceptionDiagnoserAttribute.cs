using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExceptionDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        /// <param name="displayExceptionsIfZeroValue">Display Exceptions column. True by default.</param>
        public ExceptionDiagnoserAttribute(bool displayExceptionsIfZeroValue = true)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new ExceptionDiagnoser(new ExceptionDiagnoserConfig(displayExceptionsIfZeroValue)));
        }
    }
}
