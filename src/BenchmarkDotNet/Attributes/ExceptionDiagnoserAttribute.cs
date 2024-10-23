using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using System;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExceptionDiagnoserAttribute : Attribute, IConfigSource
    {
        public IConfig Config { get; }

        /// <param name="displayExceptions">Display Exceptions column. True by default.</param>
        public ExceptionDiagnoserAttribute(bool displayExceptions = true)
        {
            Config = ManualConfig.CreateEmpty().AddDiagnoser(new ExceptionDiagnoser(new ExceptionDiagnoserConfig(displayExceptions)));
        }
    }
}